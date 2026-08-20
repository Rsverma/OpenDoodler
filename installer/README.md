# OpenDoodler Installer

Builds a Windows Installer (`.msi`) for OpenDoodler using [WiX Toolset](https://wixtoolset.org/) v7.

## Prerequisites

- .NET 10 SDK (same as the main app)
- No separate WiX install needed - `WixToolset.Sdk`/`WixToolset.UI.wixext` are restored as
  ordinary NuGet packages by the build itself.
- WiX v7 requires accepting its Open Source Maintenance Fee EULA to build (free for
  non-commercial/under-$10k-revenue use). Already handled via `<AcceptEula>wix7</AcceptEula>`
  in the wixproj - see https://wixtoolset.org/osmf/ if that stops applying to this project.

## Build

This is a two-step process: publish the app, then build the installer around that output.

1. **Publish the app** (self-contained, so the installer works on machines without .NET 10
   pre-installed - do *not* add `-p:PublishSingleFile=true`, since `Package.wxs` expects an
   ordinary "exploded" folder of files, not a bundled single-file exe):

   ```bash
   dotnet publish ../src/OpenBoardAnim/OpenBoardAnim.csproj -c Release -r win-x64 --self-contained true
   ```

   This publishes to `../src/OpenBoardAnim/bin/Release/net10.0-windows/win-x64/publish/`, which
   is `OpenBoardAnim.Installer.wixproj`'s default `PublishDir`.

2. **Build the installer:**

   ```bash
   dotnet build OpenBoardAnim.Setup.sln -c Release
   ```

   The resulting `OpenDoodlerSetup.msi` lands at
   `OpenBoardAnim.Installer/bin/Release/OpenDoodlerSetup.msi` (~150MB - it's a self-contained
   .NET publish, bundling the runtime).

To point the installer at a different publish output, or bump the version for a release, override
the relevant property instead of editing `Package.wxs`:

```bash
dotnet build OpenBoardAnim.Setup.sln -c Release -p:ProductVersion=1.1.0.0 -p:PublishDir=C:\path\to\publish\
```

## What it installs

- Every file from the publish output, under `Program Files\OpenDoodler\` (preserving folder
  structure, so `DLLs\ffmpeg.exe` etc. still resolve correctly at runtime).
- A Start Menu shortcut ("OpenDoodler"). No desktop shortcut, by design.
- Standard upgrade handling via `MajorUpgrade` - installing a newer version automatically
  removes the old one first. **Never change the `UpgradeCode` GUID in `Package.wxs`** - that's
  what makes upgrade detection work across versions.

## Caveats

Built and verified locally (WiX v7). Two things the initial (untested) draft got wrong, fixed
after a real build surfaced them:
- `InstallDir_NoLicense` isn't an actual built-in WixUI dialog set shipped in
  `WixToolset.UI.wixext` - it was a custom UI authored inside one of the toolset's own test
  fixtures, mistaken for a ready-to-use one. Using `WixUI_InstallDir` instead, which needs the
  `License.rtf` in this folder.
- Without `Compressed="yes"` / `<MediaTemplate EmbedCab="yes" />`, the build split the ~150MB
  payload across three loose external `.cab` files next to the `.msi` instead of one
  self-contained file.

There's no application-wide version number yet (see `CLAUDE.md`), so `ProductVersion` here is set
and bumped independently of the app itself for now. The MSI itself has not been run/installed -
only built - so the actual install/uninstall/upgrade experience is still unverified.
