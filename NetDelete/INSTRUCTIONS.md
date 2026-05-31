# NetDelete Utility - Instructions

This utility allows for parallel deletion of LaunchDarkly flags across different projects.

## Folder Structure

To ensure the GitHub Workflow works correctly, maintain the following structure in your repository:

```text
.github/
  workflows/
    delete-flags.yml      # The GitHub Action workflow file
LdFlagDeleter/            # The C# Project folder
  LdFlagDeleter.csproj
  Program.cs
```

## Features
- **Parallel Deletion**: Processes multiple flags concurrently.
- **Expert Error Handling**: Handles rate limits (429) automatically by respecting the `Retry-After` header.
- **Detailed Summary**: Provides a clear report of successes, failures, and missing flags at the end of the run.

*Note: If you change the folder paths, you must update the `dotnet build` and `dotnet run` commands in `delete-flags.yml`.*

## Setup

### 1. LaunchDarkly API Key
- Generate a Service Account or Personal API Token in LaunchDarkly with `delete` permissions.
- In your GitHub Repository:
  - Go to **Settings** > **Secrets and variables** > **Actions**.
  - Create a new repository secret named `LD_API_KEY` and paste your token.

### 2. GitHub Workflow
- Place the `delete-flags.yml` file into `.github/workflows/` in your main branch.

## Usage

### Local Execution
1. Set the environment variable:
   ```bash
   export LD_API_KEY="your-api-token"
   ```
2. Run the utility:
   ```bash
   dotnet run --project LdFlagDeleter/LdFlagDeleter.csproj -- \
     --SourceProject "my-project-key" \
     --FlagKeys "flag-key-1:flag-key-2:flag-key-3"
   ```

### GitHub Actions
1. Go to the **Actions** tab in your repository.
2. Select **Delete LaunchDarkly Flags** from the sidebar.
3. Click **Run workflow**.
4. Enter the `Source Project Key` and the `Flag Keys` (separated by `:`).
5. Click **Run workflow**.

## Parameters
- `--SourceProject`: The project key where the flags are located (no spaces).
- `--FlagKeys`: A list of flag keys to delete, separated by colons (`:`). Only alphanumeric characters and hyphens are allowed.
