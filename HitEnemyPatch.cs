using HarmonyLib;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Path_of_Awakening.Skills;

namespace Path_of_Awakening
{
    [HarmonyPatch]
    public class HitEnemyPatch
    {
        // 动态扫描所有怪物子类，防止有些怪物（如胡桃夹子、蜘蛛）漏掉伤害翻倍
        [HarmonyTargetMethods]
        static IEnumerable<MethodBase> ReturnTargetMethods()
        {
            var enemyTypes = typeof(EnemyAI).Assembly.GetTypes()
                .Where(t => t == typeof(EnemyAI) || t.IsSubclassOf(typeof(EnemyAI)));

            foreach (var type in enemyTypes)
            {
                var method = type.GetMethod("HitEnemy", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (method != null) yield return method;
            }
        }

        // 攻击前：记录状态并翻倍伤害
        [HarmonyPrefix]
        static void Prefix(EnemyAI __instance, ref int __0, PlayerControllerB __1, out bool __state)
        {
            __state = false; // 默认状态
            try
            {
                if (__instance != null)
                {
                    // 【核心1】：记录怪物受击前的真实死亡状态
                    __state = __instance.isEnemyDead;
                }

                if (__1 != null && __1.IsOwner)
                {
                    if (SkillManager.ActivePlayerSkills.TryGetValue(__1.playerClientId, out var skill) && skill is BloodlustSkill)
                    {
                        if (BloodlustSkill.IsActive.TryGetValue(__1.playerClientId, out bool active) && active)
                        {
                            __0 *= 2; // 真实伤害翻倍
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"[浴血奋战] Prefix Error: {e}");
            }
        }

        [HarmonyPostfix]
        static void Postfix(EnemyAI __instance, PlayerControllerB __1, bool __state)
        {
            try
            {
                if (__instance != null && __1 != null)
                {
                    if (!__state && __instance.isEnemyDead)
                    {
                        // 判定怪死了之后，只允许房主(Server)发话，防止多次重复触发
                        if (Unity.Netcode.NetworkManager.Singleton.IsServer)
                        {
                            if (SkillManager.ActivePlayerSkills.TryGetValue(__1.playerClientId, out var skill) && skill is BloodlustSkill)
                            {
                                if (BloodlustSkill.IsActive.TryGetValue(__1.playerClientId, out bool active) && active)
                                {
                                    // 房主确认击杀，向全服广播：给这个玩家加时间！
                                    SkillNetworkHandler.SendBloodlustAction(__1.playerClientId, 1);
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"[浴血奋战] Postfix Error: {e}");
            }
        }
    }
}