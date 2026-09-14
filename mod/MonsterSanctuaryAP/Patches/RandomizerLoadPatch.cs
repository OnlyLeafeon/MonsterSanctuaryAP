using HarmonyLib;
using MonsterSanctuaryAP.ArchipelagoClient;

namespace MonsterSanctuaryAP
{
    [HarmonyPatch(typeof(PlayerController), "LoadGame")]
    public static class PlayerController_LoadGame_RandomizerPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (!ApState.IsConnected) return;
            MonsterRandomizer.Initialize();
            if (MonsterSanctuaryAPPlugin._abilityrando)
            {
                SkillRandomizer.RandomizeAllMonsterSkillData();
            }
        }
    }

    [HarmonyPatch(typeof(GameController), "InitPlayerStartSetup")]
    public static class GameController_InitPlayerStartSetup_RandomizerPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (!ApState.IsConnected) return;
            MonsterRandomizer.Initialize();
            if (MonsterSanctuaryAPPlugin._abilityrando)
            {
                SkillRandomizer.RandomizeAllMonsterSkillData();
            }
        }
    }
}