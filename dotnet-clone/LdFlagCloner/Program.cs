using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

string sourceProject = "";
string destinationProject = "";
string flagsRaw = "";

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--source":
            if (i + 1 < args.Length) sourceProject = args[++i];
            break;
        case "--destination":
            if (i + 1 < args.Length) destinationProject = args[++i];
            break;
        case "--flags":
            if (i + 1 < args.Length) flagsRaw = args[++i];
            break;
    }
}

if (string.IsNullOrEmpty(sourceProject) || string.IsNullOrEmpty(destinationProject) || string.IsNullOrEmpty(flagsRaw))
{
    Console.WriteLine("Usage: LdFlagCloner --source <SourceProject> --destination <DestinationProject> --flags <FlagKeys>");
    Console.WriteLine("Example: LdFlagCloner --source my-source --destination my-dest --flags flag1:flag2:flag3");
    return;
}

string[] flagKeys = flagsRaw.Split(':');
string? apiKey = Environment.GetEnvironmentVariable("LD_API_KEY");

if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Error: LD_API_KEY environment variable is not set.");
    return;
}

// Basic validation
if (sourceProject.Contains(" ") || destinationProject.Contains(" "))
{
    Console.WriteLine("Error: Project keys must not contain spaces.");
    return;
}

foreach (var key in flagKeys)
{
    if (key.Any(c => !char.IsLetterOrDigit(c) && c != '-'))
    {
        Console.WriteLine($"Error: Flag key '{key}' contains invalid characters.");
        return;
    }
}

using HttpClient client = new HttpClient();
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(apiKey);
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

Console.WriteLine($"Starting parallel cloning of {flagKeys.Length} flags...");

await Parallel.ForEachAsync(flagKeys, async (flagKey, ct) =>
{
    await CloneFlagWithRetry(flagKey, sourceProject, destinationProject, client);
});

async Task CloneFlagWithRetry(string flagKey, string sourceProj, string destProj, HttpClient httpClient, int maxRetries = 2)
{
    int attempt = 0;
    while (attempt < maxRetries)
    {
        try
        {
            attempt++;
            Console.WriteLine($"[{flagKey}] Attempt {attempt}: Fetching flag...");
            
            var response = await httpClient.GetAsync($"https://app.launchdarkly.com/api/v2/flags/{sourceProj}/{flagKey}");
            response.EnsureSuccessStatusCode();
            Console.WriteLine($"[{flagKey}] {response.Content.ReadAsStringAsync().Result} fetched successfully."); 
            
            var content = await response.Content.ReadAsStringAsync();
            var jsonNode = JsonNode.Parse(content);

            if (jsonNode == null) throw new Exception("Failed to parse JSON response.");

            // Modify the JSON
            var obj = jsonNode.AsObject();
            
            // Remove system fields
            string[] toRemove = { "_links", "_maintainer", "environments", "creationDate", "lastModified", "_version", "includeSnippet"};
            foreach (var prop in toRemove) obj.Remove(prop);



            // Add tag "CLONNED"
            JsonArray tags = obj["tags"]?.AsArray() ?? new JsonArray();
            if (!tags.Any(t => t?.GetValue<string>() == "CLONNED"))
            {
                tags.Add("CLONNED");
            }
            obj["tags"] = tags;

            string payload = obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            
            // Save payload to file
            string fileName = $"payload_{flagKey}.json";
            await File.WriteAllTextAsync(fileName, payload);
            Console.WriteLine($"[{flagKey}] Payload saved to {fileName}");

            // Extract and save variations to variation.json
            if (obj.TryGetPropertyValue("variations", out var variationsNode) && variationsNode != null)
            {
                string variationsJson = variationsNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                string varFileName = $"variation_{flagKey}.json"; // Using flagKey to avoid collisions in parallel runs
                await File.WriteAllTextAsync(varFileName, variationsJson);
                Console.WriteLine($"[{flagKey}] Variations saved to {varFileName}");
            }

            // Create flag in destination
            Console.WriteLine($"[{flagKey}] Creating flag in {destProj}...");
            var postContent = new StringContent(payload, Encoding.UTF8, "application/json");
            var postResponse = await httpClient.PostAsync($"https://app.launchdarkly.com/api/v2/flags/{destProj}", postContent);

            if (postResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"[{flagKey}] Successfully cloned.");
                return; // Success, exit retry loop
            }
            else
            {
                var errorMsg = await postResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[{flagKey}] Failed to create: {postResponse.StatusCode} - {errorMsg}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{flagKey}] Error on attempt {attempt}: {ex.Message}");
        }

        if (attempt < maxRetries)
        {
            await Task.Delay(1000 * attempt); // Exponential backoff
        }
    }
    
    Console.WriteLine($"[{flagKey}] Max retries reached. Cloning failed.");
}
