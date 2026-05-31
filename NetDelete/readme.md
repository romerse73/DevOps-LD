# NetDelete

A specialized .NET 10 utility for bulk deleting LaunchDarkly flags in parallel with retry logic.

## Features
- **Parallel Execution**: Deletes multiple flags simultaneously using `Parallel.ForEachAsync`.
- **Retry Logic**: Automatically retries failed requests with exponential backoff.
- **GitHub Integration**: Includes a pre-configured GitHub Workflow for manual triggers.
- **Zero Dependencies**: Built strictly with standard .NET libraries.

## Quick Start
1. Ensure .NET 10 SDK is installed.
2. Set `LD_API_KEY` environment variable.
3. Run:
   ```bash
   dotnet run --project LdFlagDeleter/LdFlagDeleter.csproj -- --SourceProject <project> --FlagKeys <key1>:<key2>
   ```

For detailed setup and GitHub Action instructions, see [INSTRUCTIONS.md](./INSTRUCTIONS.md).
