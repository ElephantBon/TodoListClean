using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace TodoListClean
{
    public sealed class TodoListCleanMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony); // applies every [HarmonyPatch] in this assembly
            PUtil.InitLibrary(false);
            new POptions().RegisterOptions(this, typeof(TodoOptions)); // "Options" button in the Mods screen
            Debug.Log("[TodoListClean] loaded");
        }
    }

    /// <summary>Attach task storage to the SaveGame object (tasks are stored in the colony save).</summary>
    [HarmonyPatch(typeof(SaveGame), "OnPrefabInit")]
    internal static class SaveGame_OnPrefabInit_Patch
    {
        private static void Postfix(SaveGame __instance)
        {
            __instance.gameObject.AddOrGet<TodoStorage>();
        }
    }

    /// <summary>Create the panel once the colony has finished loading.</summary>
    [HarmonyPatch(typeof(Game), "OnSpawn")]
    internal static class Game_OnSpawn_Patch
    {
        private static void Postfix()
        {
            TodoScreen.Create();
        }
    }
}
