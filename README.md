# Folder Archive Tool

Folder Archive Tool is a Windows desktop utility for finding stale files and folders, reviewing them in a GUI, and archiving them to a destination while preserving relative paths.

The current implementation is a C# WinForms application targeting .NET 8 for Windows, with optional MSI packaging for deployment.

## What the tool does

- Scans a source folder recursively
- Finds files and folders that are older than one year (365 days)
- Uses both `LastWriteTimeUtc` and `LastAccessTimeUtc` as the age criteria
- Requires folder candidates to be older in both timestamps and to have all child files/folders also qualify
- Shows candidates in a tree view with checkboxes
- Lets you review a selected item in a preview pane
- Supports dry-run mode before moving anything
- Can move files/folders to a destination while preserving relative structure
- Can optionally compress folder candidates into `.zip` archives instead of moving the folder
- Can remove empty source folders after a move
- Offers conflict prompts for move/overwrite decisions with an "apply to all" option

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK for building from source
- Windows desktop runtime if running the installed app without the SDK
- Local filesystem access for the source and destination paths

## Install the app

### Option 1: Install from MSI

1. Download or build the MSI package.
2. Run `FolderArchiveTool.msi`.
3. Follow the Windows installer prompts.
4. Launch the app from the Start menu or installed program folder.

The MSI is created by the script in `Installer/build_msi.ps1`.

### Option 2: Build from source

From the repository root:

```powershell
dotnet build .\FolderArchiveTool\FolderArchiveTool.csproj -c Release -r win-x64
dotnet run --project .\FolderArchiveTool\FolderArchiveTool.csproj
```

## Build the packaged ZIP

To create a ZIP package of the published app:

```powershell
.\build_and_package.ps1
```

This publishes the app and creates `FolderArchiveTool_Package.zip` in the repository root.

## Build the MSI

To create an MSI for installation:

```powershell
.\Installer\build_msi.ps1 -OutputMsi "FolderArchiveTool.msi" -Configuration Release -Runtime win-x64
```

Requirements for the MSI build:

- .NET 8 SDK
- WiX Toolset installed and available in PATH
- Windows environment

## How the scanner decides a candidate

The app marks a file as a candidate when:

- `LastWriteTimeUtc` is older than 365 days
- `LastAccessTimeUtc` is also older than 365 days

The app marks a folder as a candidate when:

- the folder itself meets the same age requirement
- all descendants also meet the age requirement

This is enforced in the scanner logic in `FolderArchiveTool/ArchiveScanner.cs`.

## GUI usage

1. Enter or browse for the source directory.
2. Enter or browse for the archive destination directory.
3. Optionally set filters:
   - include file extensions
   - exclude file extensions
   - minimum and maximum size in MB
   - excluded path substrings
4. Click `Scan for candidates`.
5. Review the tree view and select the items to process.
6. Choose whether to use dry-run mode.
7. If desired, enable `Compress folder candidates to .zip`.
8. Click `Execute Move/Archive`.
9. Confirm prompts for move and destination conflicts.

## Notes on behavior

- The default age threshold is 365 days.
- The tool preserves the source-relative directory structure under the destination.
- In dry-run mode, the UI logs what would happen without changing files.
- When `Remove empty source folders after move` is enabled, empty parent folders are removed if they are under the scanned source root.
- If a destination already exists, a conflict prompt appears for that file or folder.

## Repository layout

- `FolderArchiveTool/` — WinForms desktop application
- `FolderArchiveTool.Tests/` — xUnit tests for scanner logic
- `Installer/` — WiX MSI project and build script
- `.github/workflows/ci.yml` — GitHub Actions CI build and test workflow
- `build_and_package.ps1` — ZIP packaging script

## Troubleshooting

### Build fails with WinForms target platform errors

Use the Windows-targeted framework:

```powershell
dotnet build .\FolderArchiveTool\FolderArchiveTool.csproj -c Release -r win-x64
```

### WiX / MSI build fails

Ensure WiX Toolset is installed and `heat.exe`, `candle.exe`, and `light.exe` are available in PATH.

```powershell
.\Installer\build_msi.ps1 -OutputMsi "FolderArchiveTool.msi" -Configuration Release -Runtime win-x64
```

### Source/destination access issues

Run the app with a Windows account that has permission to read the source and write to the destination, and confirm the target paths are valid.

## Safety recommendations

- Run a dry run first before moving or compressing anything.
- Verify the destination path is not inside the source folder.
- Keep a backup of important files before large archival runs.
- Review duplicate/conflict prompts carefully when files already exist at the destination.

