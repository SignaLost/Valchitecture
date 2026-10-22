# Can other mods read Valchitecture's state?

Yes. `ValchitectureAPI` is a public static class in Valchitecture.dll. Everything in it
is for the local player, is read on the main thread, and returns its "off" value before
a world loads. `ValchitectureAPI.Version` rises when members are added; existing members
keep their meaning.

## Members

| Member | Gives |
|---|---|
| `Version`, `Guid` | The API version, and the plugin GUID (`NullSignal.Valchitecture`) |
| `Enabled` | The mod's `Enabled` setting |
| `SkillId` | The Building skill's id in Mimir, `valchitecture.building` |
| `InBuildMode`, `InEditMode`, `CameraDetached` | The current mode and camera |
| `CurrentSnapMode` | `Auto`, `Manual`, `Grid` or `Off` |
| `BuiltAtLevel(wearNTear)` | The level a piece was built or last upgraded at, 0 for none |
| `IsInUseByOther(piece)` | Another player is using the piece: an open container, a cart being pulled, a seat or a bed; what `Guard Used` refuses to remove |

## The skill

The Building skill and its abilities are kept by **Mimir**. `MimirAPI.Find("building")`
gives its level and abilities, and the readers that take names, such as
`MimirAPI.IsUnlocked("building", "edit")`, work through reflection without a reference.
The abilities, in default level order: `build`, `supply`, `edit`, `mend`, `camera`,
`workshop`, `hop`, `reach`, `network`, `salvage`; the skill rewards entry has their
names. Each one's full id is the skill's and its own, such as
`valchitecture.building.camera`. A server can move any of them, so read levels from
Mimir rather than assuming these. Mimir's FAQ has the guide.

## Container stock

Building's container stock lives in **Chestworks**, as its `building` purpose:
`ChestworksAPI.Stock("building", item)` is what building can draw on beyond the
inventory, and `ChestworksAPI.IsActive("building")` whether it draws on containers right
now. Chestworks' FAQ has the guide.

## Using it without a reference

```csharp
if (BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("NullSignal.Valchitecture", out var info))
{
    var api = info.Instance.GetType().Assembly.GetType("Valchitecture.ValchitectureAPI");
    var building = api?.GetProperty("InBuildMode");
    bool inBuildMode = building != null && (bool)building.GetValue(null);
}
```
