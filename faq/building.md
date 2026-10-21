# Building from source

Works on Linux, macOS and Windows with the .NET SDK alone; no Mono or Visual Studio.

## What you need

- **.NET SDK** 8.0 or newer (`dotnet --list-sdks`). The project targets .NET Framework
  4.8 and pulls the reference assemblies for it from NuGet, so the first build needs
  network access.
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
`plugins`.
It prints what it found, publicizes the game assemblies as it goes, and installs the
whole package into `BepInEx/plugins/NullSignal.Valchitecture`, the way a mod manager would.

If any of those guesses is wrong, say so once in a `Directory.Build.props` beside the
`.csproj` (it is in `.gitignore`):

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
build compiles against and where it deploys, so point it at the profile you launch.
Any property can also be passed per build: `dotnet build -c Release -p:GamePath=...`.

## Packaging

The build also lays out `bin/Release` the way a mod manager expects, so it can be zipped
as it stands:

```
manifest.json  Valchitecture.dll  Valchitecture.pdb  icon.png  README.md  CHANGELOG.md  LICENSE.md  faq/  Translations/
```

The manifest, icon and README have to sit at the archive root. `icon.png` must be exactly
256x256. The `faq/` pages take their titles from each file's first line.

`manifest.json` is generated from the `.csproj`, so the version lives in one place:
`Version` is the release number, `Description` the blurb the mod list shows (256
characters at most), `ManifestWebsite` the repository URL, and the dependency lines in
the `PackageFiles` target pin the BepInEx pack, Jotunn, Chestworks and Mimir.

`PluginGuid` names the folder deployed into and the config file, and the DLL stays
`Valchitecture.dll`. The build fails if it stops matching the constant in
`ValchitecturePlugin.cs`, as does a version that drifts from `Version`.

## The gizmo shader

The rotation rings use their own shader, shipped as one asset bundle per platform in
`Resources/shaders_linux` and `Resources/shaders_windows`. The bundles are in the
repository, so you only rebuild them if you change `Shaders/GizmoRing.shader`.

You need Unity **6000.0.75f1 or older** (the game's own version; a bundle built by a
newer editor will not load) with the Linux and Windows Build Support modules. To
install that exact version, open `unityhub://6000.0.75f1/26349cd2a5c8`. Sign in through
the Hub once so a headless editor has a license.

```bash
./Shaders/build-shader-bundles.sh
```

The script creates a throwaway Unity project, compiles the shader headless for both
platforms and installs the bundles under `Resources/`. Put the editor and project paths
in `Shaders/build.local` (in `.gitignore`) if it cannot find them:

```sh
UNITY=/path/to/Hub/Editor/6000.0.75f1/Editor/Unity
PROJECT=/path/to/an/empty/folder
```

Then `dotnet build` as usual to embed them.

## Source layout

Only the plugin entry point sits at the root. Code the mod needs to work is in `core/`;
optional features that a setting or ability turns on are in `modules/`, with a subfolder
only for a feature of several files. Every Harmony patch is in `patch/`. Namespaces follow
folders: `core/` is `Valchitecture.Core`, `modules/BuildCamera/` is `Valchitecture.Modules.BuildCamera`.

| Folder | Contents |
|---|---|
| `api/` | The public API other mods call |
| `common/` | Every setting and keybind, logging, input gating, embedded resources, localization, prefab and piece table lookups, server detection |
| `core/` | The Building skill, Build mode and the mode framework (rotation, nudging, the bench anchor), and the `valchitecture` console command |
| `modules/` | Edit mode, building rules, reach, piece strength, the gizmo, and the features below |
| `modules/BuildCamera/` | The detached camera and the light that follows it |
| `modules/Snapping/` | Snap modes and target-point stepping; `Extra/` computes the added snap points and holds the hand-placed layouts |
| `modules/Storage/` | Building from containers and removal pickup |
| `modules/HintBar/` | The control hint bar |
| `patch/` | Every Harmony patch |
| `Resources/` | The skill icon and the shader bundles, built into the DLL |
| `Shaders/` | The gizmo shader and the Unity script that bundles it; not compiled by `dotnet` |
| `Thunderstore/` | The package icon, copied into the output folder |
| `faq/` | The mod page's FAQ entries, copied into the output folder |
| `Translations/` | The English text, one key per string; copied into the output folder and built into the DLL as a fallback |

## When a game update breaks something

A patch whose target moved fails on its own at startup; the log names it and the rest
of the mod keeps working. Decompile the game assembly to see what changed:

```bash
dotnet tool install -g ilspycmd
ilspycmd -t Player "$GamePath/valheim_Data/Managed/assembly_valheim.dll" | grep -n UpdatePlacementGhost
```

## Common failures

- **Reference assemblies for .NETFramework v4.8 were not found** — the NuGet restore
  did not run. Build once with network access.
- **Valheim / BepInEx / Jotunn not found** — the message says which path it tried; set
  that property as above.
- **Builds, but nothing happens in game** — `BepInEx/LogOutput.log` should contain
  `Loading [Valchitecture 1.0.0] (NullSignal.Valchitecture)`. If not, the DLL went to a
  different BepInEx than the one you launched; check `BepInExPath`.
- **Two plugins with the same GUID** — an older build left a loose DLL directly in
  `BepInEx/plugins`. The build now installs into `plugins/NullSignal.Valchitecture`, so
  delete the loose one once.
- **No rotation rings** — the log says why the shader bundle did not load; usually it
  was built with a newer Unity than the game runs.
