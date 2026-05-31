using System.Net.Http.Headers;
using System.Collections.Concurrent;

string sourceProject = "";
string flagsRaw = "";

// Parse parameters
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--SourceProject":
            if (i + 1 < args.Length) sourceProject = args[++i];
            break;
        case "--FlagKeys":
            if (i + 1 < args.Length) flagsRaw = args[++i];
            break;
    }
}

// Validation
if (string.IsNullOrEmpty(sourceProject) || string.IsNullOrEmpty(flagsRaw))
{
    Console.WriteLine("Usage: LdFlagDeleter --SourceProject <ProjectKey> --FlagKeys <Key1:Key2:Key3>");
    Console.WriteLine("Example: LdFlagDeleter --SourceProject my-project --FlagKeys flag-one:flag-two");
    return;
}

if (sourceProject.Contains(" "))
{
    Console.WriteLine("Error: SourceProject must not contain spaces.");
    return;
}

string[] flagKeys = flagsRaw.Split(':');
foreach (var key in flagKeys)
{
    if (key.Any(c => !char.IsLetterOrDigit(c) && c != '-'))
    {
        Console.WriteLine($"Error: Flag key '{key}' contains invalid characters (only letters, numbers, and '-' allowed).");
        return;
    }
}

string? apiKey = Environment.GetEnvironmentVariable("LD_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Error: LD_API_KEY environment variable is not set.");
    return;
}

using HttpClient client = new HttpClient();
// LaunchDarkly expects the API key in the Authorization header. 
// Using TryAddWithoutValidation to ensure we don't fail on non-standard token characters.
client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

// Concurrent collection to track results for the final summary
var results = new ConcurrentDictionary<string, string>();

Console.WriteLine($"Starting parallel deletion of {flagKeys.Length} flags from project '{sourceProject}'...");

await Parallel.ForEachAsync(flagKeys, async (flagKey, ct) =>
{
    string status = await DeleteFlagWithRetry(flagKey, sourceProject, client);
    results.TryAdd(flagKey, status);
});

// Final Summary
Console.WriteLine("\n--- Deletion Summary ---");
int successCount = 0;
int failCount = 0;
int skipCount = 0;

foreach (var kvp in results.OrderBy(x => x.Key))
{
    Console.WriteLine($"[{kvp.Key}]: {kvp.Value}");
    if (kvp.Value == "Deleted") successCount++;
    else if (kvp.Value == "Not Found") skipCount++;
    else failCount++;
}

Console.WriteLine($"\nTotal: {flagKeys.Length} | Deleted: {successCount} | Skipped: {skipCount} | Failed: {failCount}");

async Task<string> DeleteFlagWithRetry(string flagKey, string project, HttpClient httpClient, int maxRetries = 3)
{
    int attempt = 0;
    while (attempt < maxRetries)
    {
        attempt++;
        try
        {
            Console.WriteLine($"[{flagKey}] Attempt {attempt}: Deleting flag...");
            
            // Per Promptdelete.md: "header content type application/json"
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"https://app.launchdarkly.com/api/v2/flags/{project}/{flagKey}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[{flagKey}] Successfully deleted.");
                return "Deleted";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"[{flagKey}] Flag not found. Skipping.");
                return "Not Found";
            }
            else if (response.StatusCode == (System.Net.HttpStatusCode)429) // Too Many Requests
            {
                int retryAfter = 1;
                if (response.Headers.RetryAfter != null && response.Headers.RetryAfter.Delta.HasValue)
                {
                    retryAfter = (int)response.Headers.RetryAfter.Delta.Value.TotalSeconds;
                }
                Console.WriteLine($"[{flagKey}] Rate limited (429). Retrying in {retryAfter}s...");
                await Task.Delay(retryAfter * 1000);
                attempt--; // Don't count rate limits against maxRetries
                continue; 
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[{flagKey}] Failed to delete: {response.StatusCode} - {errorMsg}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{flagKey}] Error on attempt {attempt}: {ex.Message}");
        }

        if (attempt < maxRetries)
        {
            int delay = 1000 * attempt;
            Console.WriteLine($"[{flagKey}] Retrying in {delay}ms...");
            await Task.Delay(delay);
        }
    }

    Console.WriteLine($"[{flagKey}] Max retries reached. Deletion failed.");
    return "Failed";
}
