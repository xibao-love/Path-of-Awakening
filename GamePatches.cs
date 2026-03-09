using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.InputSystem;
using Path_of_Awakening.Skills;

namespace Path_of_Awakening
{
    [HarmonyPatch]
    internal class GamePatches
    {
        public static bool IsSharingDamage = false;

        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        static void OnStartOfRoundAwake()
        {
            SkillManager.CompletelyResetSkills();
            SkillNetworkHandler.Initialize();
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPrefix]
        static void OnDisconnect()
        {
            SkillManager.CompletelyResetSkills();
        }

        [HarmonyPatch(typeof(StartMatchLever), "PullLever")]
        [HarmonyPostfix]
        static void OnLeverPulled(StartMatchLever __instance)
        {
            if (__instance.leverHasBeenPulled) SkillManager.AutoAssignSkills();
        }

        [HarmonyPatch(typeof(StartOfRound), "ResetShip")]
        [HarmonyPostfix]
        static void OnShipReset()
        {
            SkillManager.ClearSkills();
        }

        [HarmonyPatch(typeof(StartOfRound), "ReviveDeadPlayers")]
        [HarmonyPostfix]
        static void OnNewDayStarted()
        {
            SkillManager.ClearSkills();
            if (SkillUI.Instance != null) SkillUI.Instance.SetPanelState(true);
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        static void OnPlayerUpdate(PlayerControllerB __instance)
        {
            if (__instance.isPlayerControlled)
            {
                SoulSymbiosisSkill.GlobalUpdate(__instance);

                if (SkillManager.ActivePlayerSkills.TryGetValue(__instance.playerClientId, out var skill))
                {
                    skill.OnUpdate(__instance);

                    // 判断按下3键
                    if (__instance.IsOwner && AwakeningInputs.Instance.UseSkillKey.triggered)
                    {
                        // 防止在打字聊天和看电脑终端时误触技能
                        if (!__instance.isTypingChat && !__instance.inTerminalMenu)
                        {
                            skill.OnActivate(__instance);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        static void OnPlayerConnect()
        {
            SkillNetworkHandler.Initialize();
        }

        [HarmonyPatch(typeof(PlayerControllerB), "DamagePlayer")]
        [HarmonyPrefix]
        static bool Prefix_DamagePlayer(PlayerControllerB __instance, ref int damageNumber, bool hasDamageSFX, bool callRPC, CauseOfDeath causeOfDeath, int deathAnimation, bool fallDamage, Vector3 force)
        {
            if (__instance.isPlayerDead) return true;

            if (__instance.IsOwner)
            {
                if (SkillManager.ActivePlayerSkills.TryGetValue(__instance.playerClientId, out var skill) && skill is IronBonesSkill)
                {
                    if (IronBonesSkill.State.TryGetValue(__instance.playerClientId, out int s) && s == 1)
                    {
                        IronBonesSkill.TriggerParry(__instance);
                        return false;
                    }
                }
            }

            if (SkillManager.ActivePlayerSkills.TryGetValue(__instance.playerClientId, out var bloodSkill) && bloodSkill is BloodlustSkill)
            {
                if (BloodlustSkill.IsActive.TryGetValue(__instance.playerClientId, out bool active) && active)
                {
                    damageNumber *= 2;
                }
            }

            if (__instance.health - damageNumber <= 0)
            {
                if (SkillManager.ActivePlayerSkills.TryGetValue(__instance.playerClientId, out var skill) && skill is MingDaoSkill mingDao)
                {
                    if (mingDao.HasTriggered.TryGetValue(__instance.playerClientId, out bool triggered) && !triggered)
                    {
                        mingDao.HasTriggered[__instance.playerClientId] = true;
                        damageNumber = __instance.health - 1;

                        if (__instance.IsOwner)
                        {
                            MingDaoSkill.TriggerEffect(__instance);
                            if (HUDManager.Instance != null)
                                HUDManager.Instance.DisplayTip("觉醒触发！", $"【{skill.Name}】为你抵挡致命一击，移速暴增！", isWarning: true);
                        }
                    }
                }
            }

            if (!IsSharingDamage && damageNumber > 0 && SoulSymbiosisSkill.ContractPairs.TryGetValue(__instance.playerClientId, out ulong partnerId))
            {
                if (__instance.IsOwner)
                {
                    PlayerControllerB partner = StartOfRound.Instance.allPlayerScripts[partnerId];
                    if (!partner.isPlayerDead)
                    {
                        int originalDamage = damageNumber;
                        int totalHealth = __instance.health + partner.health;

                        if (totalHealth <= originalDamage)
                        {
                            damageNumber = 999;
                            SkillNetworkHandler.SendSoulDamageAction(partnerId, 999);
                        }
                        else
                        {
                            int myDamage = originalDamage / 2 + originalDamage % 2;
                            int partnerDamage = originalDamage / 2;

                            if (__instance.health - myDamage <= 0)
                            {
                                int overflow = myDamage - (__instance.health - 1);
                                myDamage = __instance.health - 1;
                                partnerDamage += overflow;
                            }
                            else if (partner.health - partnerDamage <= 0)
                            {
                                int overflow = partnerDamage - (partner.health - 1);
                                partnerDamage = partner.health - 1;
                                myDamage += overflow;
                            }

                            damageNumber = myDamage;

                            if (partnerDamage > 0)
                            {
                                SkillNetworkHandler.SendSoulDamageAction(partnerId, partnerDamage);
                            }
                        }
                    }
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "KillPlayer")]
        [HarmonyPrefix]
        static bool Prefix_KillPlayer(PlayerControllerB __instance, Vector3 bodyVelocity, bool spawnBody, CauseOfDeath causeOfDeath, int deathAnimation)
        {
            if (__instance.isPlayerDead) return true;

            if (__instance.IsOwner)
            {
                if (SkillManager.ActivePlayerSkills.TryGetValue(__instance.playerClientId, out var ironSkill) && ironSkill is IronBonesSkill)
                {
                    if (IronBonesSkill.State.TryGetValue(__instance.playerClientId, out int s) && s == 1)
                    {
                        IronBonesSkill.TriggerParry(__instance);
                        return false;
                    }
                }

                if (SkillManager.ActivePlayerSkills.TryGetValue(__instance.playerClientId, out var mingDaoSkill) && mingDaoSkill is MingDaoSkill mingDao)
                {
                    if (mingDao.HasTriggered.TryGetValue(__instance.playerClientId, out bool triggered) && !triggered)
                    {
                        mingDao.HasTriggered[__instance.playerClientId] = true;

                        __instance.health = 1;
                        if (HUDManager.Instance != null)
                        {
                            HUDManager.Instance.UpdateHealthUI(1, false);
                            HUDManager.Instance.DisplayTip("觉醒触发！", $"【{mingDaoSkill.Name}】为你抵挡必杀一击，移速暴增！", isWarning: true);
                        }

                        MingDaoSkill.TriggerEffect(__instance);
                        return false;
                    }
                }
                // =============================================================
                // 3. 【新增修复】灵魂共生 (SoulSymbiosisSkill) 秒杀分摊判定
                // =============================================================
                // 前置条件：当前不是在处理分摊伤害 (防止无限死循环)，且结下了契约
                if (!IsSharingDamage && SoulSymbiosisSkill.ContractPairs.TryGetValue(__instance.playerClientId, out ulong partnerId))
                {
                    PlayerControllerB partner = StartOfRound.Instance.allPlayerScripts[partnerId];
                    if (!partner.isPlayerDead)
                    {
                        // 对方被胡桃等秒杀，等同于受到了无限大的伤害，无法被双方总血量吃下
                        // 直接给队友发送 999 点巨额伤害
                        SkillNetworkHandler.SendSoulDamageAction(partnerId, 999);
                    }
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "KillPlayer")]
        [HarmonyPostfix]
        static void Postfix_KillPlayer(PlayerControllerB __instance)
        {
            if (__instance.IsOwner && __instance.isPlayerDead)
            {
                if (SkillManager.ActivePlayerSkills.TryGetValue(__instance.playerClientId, out var skill) && skill is DeadResentmentSkill deadSkill)
                {
                    if (deadSkill.HasTriggered.TryGetValue(__instance.playerClientId, out bool triggered) && !triggered)
                    {
                        deadSkill.HasTriggered[__instance.playerClientId] = true;

                        Vector3 explosionCenter = __instance.transform.position;
                        if (__instance.deadBody != null)
                        {
                            explosionCenter = __instance.deadBody.transform.position;
                        }

                        explosionCenter += Vector3.up * 1.0f;

                        SkillNetworkHandler.SendExplosionAction(explosionCenter);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), "EquipItem")]
        [HarmonyPrefix]
        static void Prefix_EquipItem(GrabbableObject __instance)
        {
            if (__instance.playerHeldBy != null && __instance.playerHeldBy.IsOwner)
            {
                // 【新增限制】：增加 !__instance.isInShipRoom && !__instance.isInElevator
                // 防止刚进房间或读档后，在飞船里直接对以前的老废品使用金手指
                if (!__instance.hasBeenHeld &&
                    __instance.itemProperties != null &&
                    __instance.itemProperties.isScrap &&
                    !__instance.isInShipRoom &&
                    !__instance.isInElevator)
                {
                    if (SkillManager.ActivePlayerSkills.TryGetValue(__instance.playerHeldBy.playerClientId, out var skill) && skill is UnluckyMidasSkill)
                    {
                        int oldVal = __instance.scrapValue;
                        int newVal = (int)(oldVal * 1.5f);

                        __instance.SetScrapValue(newVal);
                        if (HUDManager.Instance != null)
                        {
                            HUDManager.Instance.DisplayTip("厄运金手指", $"物品价值提升: {oldVal} -> {newVal}", isWarning: false);
                        }

                        SkillNetworkHandler.SendMidasAction(__instance.NetworkObjectId, newVal);
                    }
                }
            }
        }
        // 1. 阻止玩家按 G 键丢弃魔术铲
        [HarmonyPatch(typeof(PlayerControllerB), "DiscardHeldObject")]
        [HarmonyPrefix]
        static bool Prefix_PreventMagicianShovelDrop(PlayerControllerB __instance)
        {
            if (__instance.currentlyHeldObjectServer != null)
            {
                if (MagicianSkill.PlayerToShovel.TryGetValue(__instance.playerClientId, out ulong magicId))
                {
                    if (__instance.currentlyHeldObjectServer.NetworkObjectId == magicId)
                    {
                        if (__instance.isPlayerDead) return true; // 死亡时的掉落走另一套逻辑

                        if (__instance.IsOwner)
                        {
                            HUDManager.Instance.DisplayTip("魔术师", "专属武器无法丢弃！(按技能键收回)", isWarning: true);
                        }
                        return false; // 强行阻断丢弃逻辑！
                    }
                }
            }
            return true;
        }

        // 2. 玩家死亡时，强行抹除物品栏里的魔术铲，使其不生成掉落物
        [HarmonyPatch(typeof(PlayerControllerB), "DropAllHeldItems")]
        [HarmonyPrefix]
        static void Prefix_DespawnShovelOnDeath(PlayerControllerB __instance)
        {
            if (MagicianSkill.PlayerToShovel.TryGetValue(__instance.playerClientId, out ulong magicId))
            {
                for (int i = 0; i < __instance.ItemSlots.Length; i++)
                {
                    GrabbableObject item = __instance.ItemSlots[i];
                    if (item != null && item.NetworkObjectId == magicId)
                    {
                        if (__instance.IsOwner)
                        {
                            // 通知全服，从网络上彻底抹除这个铲子
                            SkillNetworkHandler.SendMagicianAction(1, __instance.playerClientId);
                        }

                        // 在游戏把物品变成掉落物前，直接把槽位设空
                        __instance.ItemSlots[i] = null;

                        // 【关键保险】同时清空当前手持状态，防止游戏强行掉落正在手里的物品
                        if (__instance.currentlyHeldObjectServer == item)
                        {
                            __instance.currentlyHeldObjectServer = null;
                            __instance.isHoldingObject = false;
                        }
                    }
                }
            }
        }

        // 3. 禁用拾取其他武器 (普通铲子、停止标志等)
        [HarmonyPatch(typeof(PlayerControllerB), "BeginGrabObject")]
        [HarmonyPrefix]
        static bool Prefix_PreventGrabbingWeapons(PlayerControllerB __instance)
        {
            if (!__instance.IsOwner) return true;

            Ray interactRay = new Ray(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
            int interactMask = LayerMask.GetMask("Props", "InteractableObject");

            if (Physics.Raycast(interactRay, out RaycastHit hit, __instance.grabDistance, interactMask))
            {
                GrabbableObject target = hit.collider.gameObject.GetComponentInParent<GrabbableObject>();
                if (target != null)
                {
                    // 【新增拦截】：检查目标是否是场上任何人的“魔术铲”
                    if (MagicianSkill.PlayerToShovel.ContainsValue(target.NetworkObjectId))
                    {
                        // 判断这把魔术铲是不是我自己的
                        MagicianSkill.PlayerToShovel.TryGetValue(__instance.playerClientId, out ulong myMagicId);
                        if (target.NetworkObjectId != myMagicId)
                        {
                            HUDManager.Instance.DisplayTip("不可触碰", "这是魔术师的专属武器！", isWarning: true);
                            return false; // 不是自己的魔术铲，绝对不准捡！
                        }
                    }

                    // 【原有拦截】：如果我是魔术师，阻止我拾取其他近战武器
                    if (SkillManager.ActivePlayerSkills.TryGetValue(__instance.playerClientId, out var skill) && skill is MagicianSkill)
                    {
                        if (target.itemProperties.isDefensiveWeapon)
                        {
                            MagicianSkill.PlayerToShovel.TryGetValue(__instance.playerClientId, out ulong myMagicId);
                            // 只要不是我的魔术铲，其他武器一律不准捡
                            if (target.NetworkObjectId != myMagicId)
                            {
                                HUDManager.Instance.DisplayTip("魔术师的矜持", "你无法拾取其他武器！", isWarning: true);
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        // 4. 禁用挥舞其他武器 (防止别人通过终端传送等 Mod 直接把武器塞到魔术师手里)
        [HarmonyPatch(typeof(GrabbableObject), "ItemActivate")]
        [HarmonyPrefix]
        static bool Prefix_PreventSwingingOtherWeapons(GrabbableObject __instance)
        {
            PlayerControllerB player = __instance.playerHeldBy;
            if (player != null && player.IsOwner)
            {
                if (SkillManager.ActivePlayerSkills.TryGetValue(player.playerClientId, out var skill) && skill is MagicianSkill)
                {
                    if (__instance.itemProperties.isDefensiveWeapon)
                    {
                        if (MagicianSkill.PlayerToShovel.TryGetValue(player.playerClientId, out ulong magicId) && __instance.NetworkObjectId == magicId)
                        {
                            return true; // 是自己的专属铲子，允许挥舞攻击
                        }
                        HUDManager.Instance.DisplayTip("魔术师的矜持", "你无法挥动不属于你的武器！", isWarning: true);
                        return false; // 阻断攻击动作
                    }
                }
            }
            return true;
        }

    }
}