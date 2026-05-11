# Gambonanza Save Manager

A small Windows GUI tool for managing **Gambonanza** save files. It was built to make save scumming / rollback workflows easier and to inspect the current board state visually.

## Features

- Select and validate a `save.json` file manually.
- Auto-detect the default Gambonanza save path:
  - `%USERPROFILE%\AppData\LocalLow\Blukulélé\Gambonanza\save.json`
- Start/stop automatic save backups.
- Create a manual backup at any time.
- List backups with run state, wave, coins, white piece count, and black piece count.
- Restore the selected backup.
- Delete the selected backup.
- Delete all backups.
- Filter to show only non-combat backups.
- Display the board as a square-cell chess-style UI.
- Show chess pieces with Unicode chess glyphs.
- Edit current coins.
- Edit current gambits from `<CurrentGambits>`.
- Edit stock pieces from `<PiecesInStock>`.
- Chinese / English UI language switch.

## Important Notes

Always close the game before restoring or editing a save file. If the game or Steam Cloud is still running, it may overwrite your changes.

This tool creates `.bak` files before dangerous operations such as restore or save editing.

## Download / Run

Run:

```text
GambonanzaSaveManager.exe
```

The compiled executable is stored in the project root for convenience.

## Requirements

The default build is framework-dependent and requires:

- Windows
- .NET 6 Windows Desktop Runtime

If you want a portable build that can run on Windows machines without installing .NET separately, build a self-contained release.

## Build

Open PowerShell in the project directory:

```powershell
cd C:\Users\zks\Desktop\GambonanzaSaveManager
```

Framework-dependent single-file build:

```powershell
dotnet publish .\GambonanzaSaveManager.csproj -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false -o .\publish
copy .\publish\GambonanzaSaveManager.exe .\GambonanzaSaveManager.exe
```

Self-contained single-file build:

```powershell
dotnet publish .\GambonanzaSaveManager.csproj -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true -o .\publish-selfcontained
```

The self-contained executable will be larger, but it is easier to run on other computers.

## Project Structure

```text
GambonanzaSaveManager/
├─ GambonanzaSaveManager.csproj
├─ Program.cs
├─ GambonanzaSaveManager.exe
├─ README.md
└─ .gitignore
```

Generated build folders such as `bin/`, `obj/`, and `publish/` are ignored by Git.

## License

No license has been selected yet. Add a license file if you plan to publish or share the project publicly.

