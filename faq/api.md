# Can other mods read Valchitecture's state?

Yes. `Valchitecture.API` is a public static class in Valchitecture.dll. Everything in it
is for the local player, is read on the main thread, and returns its "off" value before a
world loads. `API.Version` rises when members are added; existing members keep their
meaning.

## Members

| Member | Gives |
|---|---|
| `Version`, `Guid` | The API version, and the plugin GUID (`NullSignal.Valchitecture`) |
| `Enabled` | The mod's Enabled setting |
| `SkillId` | The Building skill's id in Mimir, `valchitecture.building` |
| `InBuildMode`, `InEditMode`, `CameraDetached` | The current mode and camera |
| `CurrentSnapMode` | `Auto`, `Manual`, `Grid` or `Off` |
| `BuiltAtLevel(wearNTear)` | The level a piece was built or last upgraded at |
| `IsInUseByOther(piece)` | Another player is using the piece: an open container, a cart being pulled, a seat or a bed; what Guard Used refuses to remove |

## The skill

The Building skill and its abilities are kept by **Mimir**. `Mimir.API.Find("building")`
gives its level and its abilities; without a reference,
`Mimir.API.IsUnlocked("building", "edit")` and the other readers that take names work
through reflection. Abilities in default level order, by id: `build`, `supply`, `edit`,
`mend`, `camera`, `workshop`, `hop`, `reach`, `network`, `salvage`. Each one's full id is
the skill's and its own, such as `valchitecture.building.camera`; the skill rewards FAQ
lists their names.
A server can move any of them, so read levels from Mimir rather than assuming these.
Mimir's FAQ has the guide.

## Container stock

Building's container stock lives in **Chestworks**, as its `building` purpose:
`Chestworks.API.Stock("building", item)` is what building can draw on beyond the
inventory, and `Chestworks.API.IsActive("building")` whether it draws on containers right
now. Chestworks' FAQ has the guide.

## Using it without a reference

```csharp
if (BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("NullSignal.Valchitecture", out var info))
{
    var api = info.Instance.GetType().Assembly.GetType("Valchitecture.API");
    var building = api?.GetProperty("InBuildMode");
    bool inBuildMode = building != null && (bool)building.GetValue(null);
}
```
