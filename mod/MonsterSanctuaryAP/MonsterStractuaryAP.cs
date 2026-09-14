using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using eradev.monstersanctuary.ModsMenuNS;
using HarmonyLib;
using MonsterSanctuaryAP.ArchipelagoClient;
using UnityEngine;

namespace MonsterSanctuaryAP
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MonsterSanctuaryAPPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.slotlock.monstersanctuaryap";
        public const string PluginName = "MonsterSanctuaryAP";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;
        private Harmony harmony;

        public static event Action<int> OnChestOpened;

        internal static ConfigEntry<string> CfgHost;
        internal static ConfigEntry<string> CfgSlot;
        internal static ConfigEntry<string> CfgPassword;

        public static int _expmult = 1000;
        public static bool _starterrando = true;
        public static bool _monsterrando = true;
        public static bool _abilityrando = true;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginName} version {PluginVersion} is loading...");

            CfgHost = Config.Bind("Connection", "HostName", "", "Archipelago server address (e.g. archipelago.gg:38281)");
            CfgSlot = Config.Bind("Connection", "SlotName", "", "Your slot name");
            CfgPassword = Config.Bind("Connection", "Password", "", "Server password (leave blank if none)");

            ModsMenu.RegisterOptionsEvt += (_, _) =>
            {
                ModsMenu.TryAddOption(
                    modName: PluginName,
                    optionName: "EXP Multiplication",
                    displayValueFunc: () => _expmult.ToString(),
                    possibleValuesFunc: () => new List<string> { "1", "1000", "2000", "5000", "10000", "50000", "100000" },

                    // Use the string selection callback to parse and set your value safely!
                    onValueSelectFunc: (string selectedValue) =>
                    {
                        if (int.TryParse(selectedValue, out int result))
                        {
                            _expmult = result;
                            ModsMenu.LogInfo($"Value updated to: {_expmult}");
                        }
                    },

                    // This is safe to leave null now that onValueSelectFunc is defined
                    onValueChangeFunc: null,

                    setDefaultValueFunc: () =>
                    {
                        _expmult = 1000;
                    }
                );
                ModsMenu.TryAddOption(
                    modName: PluginName,
                    optionName: "Starter Monster Randomizer",
                    displayValueFunc: () => _starterrando ? "Yes" : "No",
                    possibleValuesFunc: () => new List<string> { "Yes", "No" },

                    // Use the string selection callback to parse and set your value safely!
                    onValueSelectFunc: (string selectedValue) =>
                    {
                        _starterrando = selectedValue == "Yes";
                    },

                    // This is safe to leave null now that onValueSelectFunc is defined
                    onValueChangeFunc: null,

                    setDefaultValueFunc: () =>
                    {
                        _starterrando = true;
                    }
                );
                ModsMenu.TryAddOption(
                    modName: PluginName,
                    optionName: "World Monster Randomizer",
                    displayValueFunc: () => _monsterrando ? "Yes" : "No",
                    possibleValuesFunc: () => new List<string> { "Yes", "No" },

                    // Use the string selection callback to parse and set your value safely!
                    onValueSelectFunc: (string selectedValue) =>
                    {
                        _monsterrando = selectedValue == "Yes";
                    },

                    // This is safe to leave null now that onValueSelectFunc is defined
                    onValueChangeFunc: null,

                    setDefaultValueFunc: () =>
                    {
                        _monsterrando = true;
                    }
                );
                ModsMenu.TryAddOption(
                    modName: PluginName,
                    optionName: "Ability Randomizer",
                    displayValueFunc: () => _abilityrando ? "Yes" : "No",
                    possibleValuesFunc: () => new List<string> { "Yes", "No" },

                    onValueSelectFunc: (string selectedValue) =>
                    {
                        _abilityrando = selectedValue == "Yes";
                    },

                    onValueChangeFunc: null,

                    setDefaultValueFunc: () =>
                    {
                        _abilityrando = true;
                    }
                );
            };

            try
            {
                harmony = new Harmony(PluginGUID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.LogInfo("Harmony patches applied successfully!");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to initialize Harmony patches: {ex.Message}");
            }

            GameController.LevelCap = 100;

            MonsterSanctuaryAP.Mapping.Locations.Load();
            RoomCheckTracker.Load();

            CreateUI();
        }

        private void CreateUI()
        {
            if (Patcher.UI != null)
                return;

            var guiObject = new GameObject("Archipelago UI");
            guiObject.AddComponent<ArchipelagoUI>();
            UnityEngine.Object.DontDestroyOnLoad(guiObject);
            Patcher.UI = guiObject.GetComponent<ArchipelagoUI>();

            Log.LogInfo("Archipelago UI created");
        }

        internal static void TriggerChestOpened(int chestId)
        {
            OnChestOpened?.Invoke(chestId);
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }
    }

    public static class Patcher
    {
        public static ArchipelagoUI UI { get; set; }
    }
}
