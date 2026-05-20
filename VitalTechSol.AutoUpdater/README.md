# VitalTechSol.AutoUpdater

A reusable GitHub auto-updater library for Windows Forms applications targeting .NET 8.

## Features

- Automatically checks for updates from GitHub releases
- Downloads and applies updates with minimal user interaction
- Configurable for any GitHub repository
- Supports manual and automatic update checks

## Installation

### Option 1: NuGet Package (Local)

Build the package and add it to your local NuGet source:

```bash
dotnet pack VitalTechSol.AutoUpdater.csproj -c Release
```

Then add to your project:

```bash
dotnet add package VitalTechSol.AutoUpdater --source ./bin/Release
```

### Option 2: Project Reference

Add a project reference to your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\VitalTechSol.AutoUpdater\VitalTechSol.AutoUpdater.csproj" />
</ItemGroup>
```

## Usage

```csharp
using VitalTechSol.AutoUpdater;

// Initialize the updater with your repo information
var updater = new AutoUpdater(
    repoUrl: "https://api.github.com/repos/YourOrg/YourRepo/releases/latest",
    appName: "YourAppName",
    executableName: "YourApp.exe",
    currentVersionStr: Application.ProductVersion
);

// Check for updates (automatic on startup)
await updater.CheckForUpdatesAsync(manualCheck: false);

// Or manual check (shows "up to date" message)
await updater.CheckForUpdatesAsync(manualCheck: true);
```

## Requirements

- .NET 8 or higher
- Windows Forms
- GitHub repository with releases and assets (zip files)

## How It Works

1. Queries the GitHub API for the latest release
2. Compares version tags with the current application version
3. If a newer version is found, prompts the user to update
4. Downloads the release asset (zip file)
5. Extracts the update to a temporary directory
6. Creates a batch script to replace files and restart the app
7. Exits the current application and runs the update script

## License

Use freely in your projects.
