# File Archive Utility

A Windows desktop utility for scanning stale files and folders, reviewing candidates in a tree view, and moving or archiving them to a target destination with filtering and safety checks.

## Project status

This project is now implemented as a .NET 8 WinForms desktop application under the FolderArchiveTool project. The app includes a modern dashboard-style interface, candidate review, destination prompts, overwrite handling, and live scan results.

## Included project files

- `FolderArchiveTool/` — WinForms application code.
- `FolderArchiveTool.Tests/` — automated tests for the archive scanner and filtering logic.
- `Installer/` — MSI packaging assets.
- `Archive-OldFiles-GUI.ps1` — legacy PowerShell script retained for reference.
- `README.md` — project overview, setup, and usage instructions.

## Core features

- Browse a source folder and destination folder.
- Scan stale files and folders using a UTC-based age threshold.
- Review matched candidates in a checked tree view.
- Filter results by:
  - include extensions
  - exclude extensions
  - minimum and maximum file size
  - excluded path names
- Preview selected file or folder details before execution.
- Run in dry-run mode for safe testing.
- Optionally remove empty folders after processing.
- Compress folder candidates into ZIP archives instead of moving them.
- Support overwrite decisions for conflicting destination files or folders.
- Optionally apply a move choice to remaining prompts.
- Keep a live log of actions and errors.

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK
- Access permissions to the source and destination folders

## Build and run

From the project root, run:

```powershell
dotnet restore
```

Then build:

```powershell
dotnet build "FolderArchiveTool/FolderArchiveTool.csproj"
```

To run the application:

```powershell
dotnet run --project "FolderArchiveTool/FolderArchiveTool.csproj"
```

> WinForms desktop apps require a Windows environment. This project targets Windows and is configured for Windows desktop execution.

## How to use the GUI

### 1. Choose the source folder

Select the folder you want the tool to inspect.

### 2. Choose the destination folder

Select the archive target where files or folders should be moved or compressed.

### 3. Scan for candidates

Click the Scan for candidates button to evaluate the source tree against the current filters and age threshold.

### 4. Review matches

The tree view shows file and folder candidates. You can check or uncheck items before execution.

### 5. Use filters

The filter controls let you narrow the set of candidates:

- Include extensions: only consider matching extensions
- Exclude extensions: ignore files by extension
- Min size and Max size: restrict by file size in MB
- Exclude paths: skip folders or names that match a path fragment

### 6. Preview item details

Selecting a node in the tree shows information such as file size and timestamp details in the preview panel.

### 7. Execute the operation

Click Execute Move/Archive to perform the action for the selected candidates.

If Dry run is enabled, the log will show what would happen without moving files.

## Safety behavior

The tool is designed to help prevent accidental data loss:

- Move confirmations appear before each move action.
- Overwrite conflicts can be resolved with per-item or all remaining choices.
- Empty folders are only removed after the move step succeeds.
- ZIP compression can be used instead of moving folder trees.

## Log output

The application writes operation updates to the log panel in the GUI. The tool also surfaces action messages and errors while scanning and moving files.

## Notes

- The project includes a legacy PowerShell script that may still be useful as a reference for older workflows.
- The current primary implementation is the WinForms desktop project in `FolderArchiveTool`.
- The scanner uses last write and last access timestamps older than the configured threshold, then applies the selected filters.

## Build verification

The project was successfully verified with:

```powershell
dotnet build "FolderArchiveTool/FolderArchiveTool.csproj"
```

The current build reported 0 errors and succeeded on .NET SDK 8.0.425.


If Windows blocks the script, use:

```powershell
Unblock-File -Path "C:\Scripts\FileArchiveUtility\Archive-OldFiles-GUI.ps1"
```

You can also launch it with:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "C:\Scripts\FileArchiveUtility\Archive-OldFiles-GUI.ps1"
```

The `Bypass` option applies only to that PowerShell process and does not permanently change the computer's execution policy.

## Recommended Testing Procedure

Before using the utility on production data:

1. Create a small test source folder.
2. Add a few files with different modified dates.
3. Select a test archive destination.
4. Run with Dry Run enabled.
5. Review the status window and CSV report.
6. Run Live mode using the test data.
7. Confirm that the folder structure was preserved.
8. Confirm that newer files remained in the source location.
9. Confirm that only empty folders were removed.
10. Run a Dry Run against production data before enabling Live mode.

## Changing File Dates for Testing

You can create a test file and set its last modified date using PowerShell:

```powershell
New-Item -Path "C:\ArchiveTest\OldFile.txt" -ItemType File -Force
(Get-Item "C:\ArchiveTest\OldFile.txt").LastWriteTime = (Get-Date).AddDays(-400)
```

Create a newer file:

```powershell
New-Item -Path "C:\ArchiveTest\NewFile.txt" -ItemType File -Force
(Get-Item "C:\ArchiveTest\NewFile.txt").LastWriteTime = (Get-Date).AddDays(-30)
```

Set the GUI threshold to 365 days. Only `OldFile.txt` should be eligible.

## Important Safety Notes

- Always run in Dry Run mode first.
- Review the CSV report before enabling Live mode.
- Ensure the archive destination has sufficient free storage.
- Confirm backup and retention policies before moving production files.
- Do not archive folders subject to legal hold, active investigations, or compliance restrictions.
- Test access using the same account that will run the application.
- Avoid using mapped drive letters for scheduled or service accounts. UNC paths are more reliable.
- The script moves files. A successful move removes the file from the source location.

## Troubleshooting

### The GUI Does Not Open

Run the script using STA mode:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File ".\Archive-OldFiles-GUI.ps1"
```

### Access Is Denied

Verify that the account has permissions on both the source and destination shares.

Test access:

```powershell
Test-Path "\\FileServer01\Data"
Test-Path "\\ArchiveServer01\Archive\Data"
```

### The Source Path Is Not Found

Confirm:

- The server is online.
- DNS resolves the server name.
- The share name is correct.
- The account has permission to list the share.

### Files Are Being Skipped

Check whether:

- The destination file already exists.
- The file is newer than the cutoff date.
- The file extension is excluded.
- The file is located in an excluded folder.
- The account cannot access the file.

### Logs Cannot Be Created

The default log directory is:

```text
C:\ProgramData\FileArchiveUtility\Logs
```

Run PowerShell with sufficient local permissions or change the log path in the script.

## Scheduling Consideration

This version is designed as an interactive GUI application. For unattended scheduled execution, use a separate non-GUI version or modify the script to accept command-line parameters.

A Windows Scheduled Task should normally use a service account with permissions to both UNC paths.
