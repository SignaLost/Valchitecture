# How do I build it from source?

Works on Linux, macOS and Windows with the .NET SDK alone; no Mono or Visual Studio.

## What you need

- **.NET SDK** 8.0 or newer (`dotnet --list-sdks`). The project targets .NET Framework
  4.8 and pulls its reference assemblies from NuGet, so the first build needs network
  access.
- **Valheim** installed.
- **BepInEx**, **Jotunn**, **Chestworks** and **Mimir** installed where the mod will run:
  the game folder for a manual install, or a mod manager profile. Building Chestworks and
  Mimir from source deploys them there.

## Build

```bash
dotnet build -c Release
```

The build finds Valheim in the usual Steam locations, BepInEx in the game folder or a
Gale/r2modman `Default` profile, and Jotunn, Chestworks and Mimir in that BepInEx's
`plugins`. It prints what it found, publicizes the game assemblies, and installs the
package into `BepInEx/plugins/NullSignal-Valchitecture`, the folder a mod manager uses.

If a guess is wrong, say so once in a `Directory.Build.props` beside the `.csproj`
(it is in `.gitignore`):

```xml
<Project>
  <PropertyGroup>
    <GamePath>/path/to/steamapps/common/Valheim</GamePath>
    <BepInExPath>/path/to/profile/BepInEx</BepInExPath>
    <!-- Only if Jotunn, Chestworks or Mimir is not in that profile's plugins: -->
    <!-- <JotunnPath>/path/to/Jotunn.dll</JotunnPath> -->
    <!-- <ChestworksPath>/path/to/Chestworks.dll</ChestworksPath> -->
    <!-- <MimirPath>/path/to/Mimir.dll</MimirPath> -->
  </PropertyGroup>
</Project>
```

`GamePath` is the folder that contains `valheim_Data`. `BepInExPath` is both what the
build compiles against and where it deploys, so point it at the profile you launch. Any
property can also be passed per build: `dotnet build -c Release -p:GamePath=...`.

## Packaging

The build lays out `bin/Release` the way a mod manager expects, so it can be zipped
as it stands:

```
manifest.json  Valchitecture.dll  Valchitecture.pdb  icon.png  README.md  CHANGELOG.md  LICENSE.md  faq/  Translations/
```

`manifest.json` is generated from the `.csproj`, so the version lives in one place:
`Version` is the release number, `Description` the blurb the mod list shows (256
characters at most), `ManifestWebsite` the repository URL, and the dependency lines in
the `PackageFiles` target pin the BepInEx pack, Jotunn, Chestworks and Mimir.

`PluginGuid` names the config file and `PluginFolder` the folder deployed into; the DLL
stays `Valchitecture.dll`. The build fails if it stops matching the constant in
`ValchitecturePlugin.cs`, as does a version that drifts from `Version`.

## The shaders

The rotation rings and the snap point bubbles use their own shaders,
`Shaders/GizmoRing.shader` and `Shaders/SnapPoint.shader`, shipped together as one asset
bundle per platform in
`Resources/shaders_linux` and `Resources/shaders_windows`. The bundles are in the
repository, so you only rebuild them if you change a shader.

You need a Unity editor on the game's own 6000.0 stream (a bundle built by a newer
editor will not load) with the Linux and Windows Build Support modules, signed in
through the Hub once so a headless editor has a license.

```bash
./Shaders/build-shader-bundles.sh
```

The script creates a throwaway Unity project, compiles the shaders headless for both
platforms and installs the bundles under `Resources/`. Put the editor and project paths
in `Shaders/build.local` (in `.gitignore`) if it cannot find them:

```sh
UNITY=/path/to/Hub/Editor/6000.0.75f1/Editor/Unity
PROJECT=/path/to/an/empty/folder
```

Then `dotnet build` as usual to embed them.

## Source layout

Only the plugin entry point sits at the root. Code the mod needs to work is in `core/`;
features that a setting or ability turns on are in `modules/`, with a subfolder for a
feature of several files. Namespaces follow folders: `core/` is `Valchitecture.Core`,
`modules/BuildCamera/` is `Valchitecture.Modules.BuildCamera`.

| Folder | Contents |
|---|---|
| `api/` | The public API other mods call |
| `common/` | Every setting and keybind, logging, input gating, embedded resources, localization, prefab and piece table lookups, server detection |
| `core/` | The Building skill and its abilities, experience, the mode framework (rotation, nudging, the station anchor, reach rules, build costs), and the `valchitecture` console command |
| `modules/` | Edit mode, building rules, reach, piece strength, the in-use guard and the gizmo |
| `modules/BuildCamera/` | The detached camera and the light that follows it |
| `modules/Snapping/` | Snap modes and target point stepping; `Extra/` computes the added snap points and holds the hand-placed layouts |
| `modules/Storage/` | Building from containers and removal pickup |
| `modules/HintBar/` | The control hint bar |
| `patch/` | Every Harmony patch |
| `Resources/` | The skill icon and the shader bundles, built into the DLL |
| `Shaders/` | The gizmo and snap point shaders and the Unity script that bundles them; not compiled by `dotnet` |
| `Thunderstore/` | The package icon, copied into the output folder |
| `faq/` | These pages, copied into the output folder |
| `Translations/` | The English text, one key per string; copied into the output folder and built into the DLL as a fallback |

## When a game update breaks something

A patch whose target moved fails on its own at startup; the log names it and the rest
of the mod keeps working. Decompile the game assembly to see what changed:

```bash
dotnet tool install -g ilspycmd
ilspycmd -t Player "$GamePath/valheim_Data/Managed/assembly_valheim.dll" | grep -n UpdatePlacementGhost
```

## Common failures

- **Reference assemblies for .NETFramework v4.8 were not found**: the NuGet restore did
  not run. Build once with network access.
- **Valheim / BepInEx / Jotunn / Chestworks / Mimir not found**: the message says which
  path it tried; set that property as above.
- **Builds, but nothing happens in game**: `BepInEx/LogOutput.log` should contain
  `Loading [Valchitecture` and the version. If not, the DLL went to a different BepInEx
  than the one you launched; check `BepInExPath`.
- **Two plugins with the same GUID**: an older build left a copy behind, either loose in
  `BepInEx/plugins` or in a `NullSignal.Valchitecture` folder. The build installs into
  `plugins/NullSignal-Valchitecture`, so delete the old one. The same goes for Chestworks
  and Mimir.
- **No rotation rings**: the log says why the shader bundle did not load; usually it was
  built with a newer Unity than the game runs.
