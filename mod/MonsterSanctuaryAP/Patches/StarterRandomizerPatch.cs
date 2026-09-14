using System;
using HarmonyLib;
using MonsterSanctuaryAP.ArchipelagoClient;
using UnityEngine;

namespace MonsterSanctuaryAP
{
    [HarmonyPatch(typeof(KeepersIntro), "Start")]
    public static class KeepersIntroStartPatch
    {
        [HarmonyPrefix]
        public static void Prefix(KeepersIntro __instance)
        {
            if (GameController.Instance == null) return;
            if (!ApState.IsConnected) return;
            if (!MonsterSanctuaryAPPlugin._starterrando) return;
            if (ProgressManager.Instance.GetBool("FamiliarChoiceCompleted")) return;
            if (__instance.FamiliarButtons == null || __instance.FamiliarButtons.Count == 0) return;

            MonsterRandomizer.Initialize();
            if (!MonsterRandomizer.IsInitialized) return;

            var rng = new System.Random(ApState.GetSeedHash());

            for (int i = 0; i < __instance.FamiliarButtons.Count; i++)
            {
                SelectFamiliarButton button = __instance.FamiliarButtons[i];
                if (button == null) continue;

                GameObject randomPrefab = MonsterRandomizer.GetReplacementMonster(button.FamiliarPrefab) ?? MonsterRandomizer.GetReplacementAt(i);
                if (randomPrefab == null) continue;

                button.FamiliarPrefab = randomPrefab;

                Monster monsterComponent = randomPrefab.GetComponent<Monster>();
                if (monsterComponent != null)
                {
                    if (randomPrefab.GetComponent<MonsterBehavior>() == null)
                    {
                        randomPrefab.AddComponent<MonsterBehavior>();
                        Debug.LogWarning($"[Randomizer] Added missing MonsterBehavior to {randomPrefab.name}");
                    }

                    if (randomPrefab.GetComponent<SkillManager>() == null)
                    {
                        randomPrefab.AddComponent<SkillManager>();
                        Debug.LogWarning($"[Randomizer] Added missing SkillManager to {randomPrefab.name}");
                    }

                    var familiarComp = randomPrefab.GetComponent<MonsterFamiliar>();
                    if (familiarComp == null)
                    {
                        familiarComp = randomPrefab.AddComponent<MonsterFamiliar>();
                        familiarComp.FamiliarType = (EFamiliar)rng.Next(0, 4);
                    }

                    if (button.FamiliarSprite != null && monsterComponent.Sprite != null)
                    {
                        tk2dSprite buttonSpriteComponent = button.FamiliarSprite.GetComponent<tk2dSprite>();
                        if (buttonSpriteComponent != null)
                        {
                            buttonSpriteComponent.SetSprite(monsterComponent.Sprite.spriteId);
                        }
                    }
                }

                Debug.Log($"[Randomizer] Successfully swapped starter slot {i} to: {randomPrefab.name}");
            }
        }
    }
}