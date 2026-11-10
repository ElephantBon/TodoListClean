# Todo List Clean — Oxygen Not Included mod

> **Disclaimer:** This mod was developed by Claude, an AI model made by Anthropic.

Todo List Clean is a to-do panel docked directly above the game's warning notifications ("No outhouse built" etc.).
Each task remembers a camera view; click it to fly there. Editing controls only appear after
pressing the gear in the panel header.

## Panel

```
[v] To Do                 [+] [gear]
    Build second electrolyzer room
    Hatch ranch
    Check the cool steam vent
```

Settings view (gear pressed):

```
[v] To Do                 [+] [gear]
    Build second electro...  [top][^][v][bottom][camera][x]
    ...
```

- **Click a task** to move the camera to its saved view (switches asteroid in Spaced Out).
- **+** adds a task at the current view; type the name, Enter saves, Esc cancels.
- **Header title** collapses / expands the list.
- **Gear** turns on settings view. There, clicking a task's name edits it in place
  (Enter saves, Esc cancels) instead of moving the camera, and each task shows
  move to top / up / down / to bottom, camera (save current view) and x (delete). Delete a task when it's done.
- **Item background opacity** (0-100%, default 10%; 0 = no background) is in the mod's
  **Options** button in the main-menu Mods screen. It is saved to `config.json` in the
  mod folder and applied when you load a colony.
- Text uses the same font as the game's Diagnostics panel.
- Tasks are written immediately to `tasks\<colony name>.json` in the mod folder, so they
  survive quitting without saving or loading an older autosave. A copy is also kept inside
  the colony save and used only when that JSON file doesn't exist yet.

## Build in Visual Studio (Windows)

Uses `netstandard2.1`, paths in `Directory.Build.props`,
installs to `mods\Local`, PLib merged in with ILRepack. Visual Studio restores the PLib and
ILRepack NuGet packages on the first build; if it doesn't, right-click the solution →
**Restore NuGet Packages**.

1. Open **`Directory.Build.props`** and check `GameFolder`. It is pre-filled with the path:
   `D:\SteamLibrary\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed`.
   If Documents is in OneDrive, change `ModFolder` as the comment says.
2. Double-click **`TodoListClean.sln`** in Visual Studio 2022.
3. **Build → Rebuild Solution**. Debug or Release both work.
4. The Output window ends with `TodoListClean installed to ...\mods\Local\DEV_TodoListClean`.
5. Start the game, open **Mods**, enable **Todo List Clean**, restart.

If `GameFolder` is wrong, the build stops with one error naming the folder it looked in.
Fix `Directory.Build.props` and right-click the solution → **Reload**.

Build with the game closed (it locks the DLL while running).
Crash log: `%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\Player.log`

## If a game update breaks the build

The mod touches only a handful of game APIs, all in `src/TodoScreen.cs` and `src/Mod.cs`:

- `CameraController.Instance.SetTargetPos(Vector3, float, bool)`
- `CameraController.Instance.ActiveWorldStarWipe(int, Vector3, float, System.Action)`
- `ClusterManager.Instance.activeWorldId` / `GetWorld(int)`
- `NotificationScreen.Instance` (panel is placed just above it)
- `Localization.GetFont` / `LocText` (font lookup)
- `DlcManager.IsExpansion1Active()`
- `GameScreenManager.Instance.ssOverlayCanvas`
- `KScreen` (`OnKeyDown`, `OnKeyUp`, `GetSortKey`, `ConsumeMouseScroll`, `Activate`)
- Harmony patches on `SaveGame.OnPrefabInit` and `Game.OnSpawn`

If one changes signature, the compiler error will point straight at it.

## Files

```
TodoListClean.sln   open this in Visual Studio
Directory.Build.props  YOUR paths (GameFolder / ModFolder)
mod.yaml            title / description / staticID
mod_info.yaml       version, API version, DLC support (all)
TodoListClean.csproj
src/Mod.cs          UserMod2 entry, PLib options registration, Harmony patches
src/TodoOptions.cs  Options screen fields (config.json)
Directory.Build.targets  merges PLib.dll into the mod DLL
src/TodoStorage.cs  task model, saved as JSON in the save file
src/TodoScreen.cs   the panel (header, rows, settings mode, actions)
src/UIKit.cs        uGUI/TMP builder, game-font lookup, pixel icons
```
