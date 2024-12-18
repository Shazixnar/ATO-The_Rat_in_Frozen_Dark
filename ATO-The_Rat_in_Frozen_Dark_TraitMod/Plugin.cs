using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Obeliskial_Essentials;
using static Obeliskial_Essentials.Essentials;

namespace TheRatinFrozenDark
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.stiffmeds.obeliskialessentials")]
    [BepInDependency("com.stiffmeds.obeliskialcontent")]
    [BepInProcess("AcrossTheObelisk.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal const int ModDate = 20241218;
        private readonly Harmony harmony = new(PluginInfo.PLUGIN_GUID);
        internal static ManualLogSource Log;
        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginInfo.PLUGIN_GUID} {PluginInfo.PLUGIN_VERSION} has loaded!");
            // register with Obeliskial Essentials
            RegisterMod(
                _name: PluginInfo.PLUGIN_NAME,
                _author: "Shazixnar",
                _description: "Zek is better at curse or corrupting Cold cards now.",
                _version: PluginInfo.PLUGIN_VERSION,
                _date: ModDate,
                _link: @"https://across-the-obelisk.thunderstore.io/package/Shazixnar/The_Rat_in_Frozen_Dark/",
                _contentFolder: "The Rat in Frozen Dark",
                _type: new string[5] { "content", "hero", "trait", "card", "perk" }
            );
            medsTexts["custommainperkdark2c"] = "Dark explosion on enemies deals 0.7 more damage per charge.";
            medsTexts["custommainperkchill2d"] = "Chill on enemies increases Dark explosion 0.1 more damage per 20 charges.";
            // apply patches
            harmony.PatchAll();
        }
    }
}
