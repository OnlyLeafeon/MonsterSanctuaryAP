using System;
using System.Reflection;
using HarmonyLib;

namespace MonsterSanctuaryAP
{
    [HarmonyPatch(typeof(ExpScreen), "StartExpScreen")]
    public static class ExpScreen_StartExpScreen_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ExpScreen __instance)
        {
            if (__instance == null) return;

            try
            {
                FieldInfo expRewardField = typeof(ExpScreen).GetField("expReward", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (expRewardField == null) return;

                int baseExpReward = (int)expRewardField.GetValue(__instance);

                int multipliedExpReward = baseExpReward * MonsterSanctuaryAPPlugin._expmult;
                expRewardField.SetValue(__instance, multipliedExpReward);

                int additionalExpToGrant = multipliedExpReward - baseExpReward;
                if (additionalExpToGrant > 0)
                {
                    ProgressManager.Instance?.AddExpMonsterArmy(additionalExpToGrant);
                }

                if (__instance.HeaderText != null)
                {
                    __instance.HeaderText.text = Utils.LOCA("Exp reward", ELoca.UI) + ": " + multipliedExpReward;
                }
            }
            catch (Exception ex)
            {
                MonsterSanctuaryAPPlugin.Log.LogError($"[AP Patch] Error applying ExpReward multiplier: {ex.Message}");
            }
        }
    }
}
