using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;

namespace Path_of_Awakening.Skills
{
    public class BloodVitalitySkill : AwakeningSkill
    {
        public override string Id => "skill_bloodvitality";
        public override string Name => "【被动可进阶】血之活力";
        public override string Description => "体力濒竭时继续奔跑抽取生命。累计耗血30/60/90点将获得【滋养Ⅰ/Ⅱ/Ⅲ】，永久提升移速与体力恢复。重伤自愈上限降为10%。";

        public static Dictionary<ulong, bool> HasWarned = new Dictionary<ulong, bool>();
        public static Dictionary<ulong, int> LastHealth = new Dictionary<ulong, int>();
        // 用于限制扣血频率的计时器
        public static Dictionary<ulong, float> SprintTimer = new Dictionary<ulong, float>();

        // ================= 滋养系统状态记录 =================
        public static Dictionary<ulong, int> ConsumedHealth = new Dictionary<ulong, int>();
        public static Dictionary<ulong, int> NourishmentLevel = new Dictionary<ulong, int>();

        public override void OnApply(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            HasWarned[id] = false;
            LastHealth[id] = player.health;
            SprintTimer[id] = 0f;

            // 初始化滋养状态
            ConsumedHealth[id] = 0;
            NourishmentLevel[id] = 0;
        }

        public override void OnRemove(PlayerControllerB player)
        {
            ulong id = player.playerClientId;

            // 移除技能时，如果身上有滋养的移速加成，必须归还移速
            if (NourishmentLevel.TryGetValue(id, out int level) && player.IsOwner)
            {
                if (level == 1) player.movementSpeed /= 1.1f;
                else if (level == 2) player.movementSpeed /= 1.2f;
                else if (level == 3) player.movementSpeed /= 1.3f;
            }

            HasWarned.Remove(id);
            LastHealth.Remove(id);
            SprintTimer.Remove(id);
            ConsumedHealth.Remove(id);
            NourishmentLevel.Remove(id);
        }

        public override void OnUpdate(PlayerControllerB player)
        {
            if (!player.IsOwner) return;
            ulong id = player.playerClientId;

            if (!HasWarned.ContainsKey(id)) HasWarned[id] = false;
            if (!LastHealth.ContainsKey(id)) LastHealth[id] = player.health;
            if (!SprintTimer.ContainsKey(id)) SprintTimer[id] = 0f;
            if (!ConsumedHealth.ContainsKey(id)) ConsumedHealth[id] = 0;
            if (!NourishmentLevel.ContainsKey(id)) NourishmentLevel[id] = 0;

            // ================= 1. 重伤恢复上限降为 10% =================
            int currentHealth = player.health;
            if (currentHealth > LastHealth[id])
            {
                int diff = currentHealth - LastHealth[id];
                // 如果是原版游戏底层的缓慢回血（每次+1），且当前血量超过10
                if (diff == 1 && currentHealth > 10 && currentHealth <= 20)
                {
                    // 撤销这次原版回血，保持你当前的真实血量
                    player.health = LastHealth[id];
                    currentHealth = LastHealth[id];
                    // 刷新一下UI，把屏幕上刚才跳动的 +1 盖回去
                    HUDManager.Instance.UpdateHealthUI(player.health, false);
                }
            }
            LastHealth[id] = currentHealth;

            // 接管游戏底层的“骨科治愈”判定，强行解除瘸腿
            if (currentHealth >= 10 && player.criticallyInjured)
            {
                player.MakeCriticallyInjured(false);
            }


            // ================= 滋养 III：额外体力恢复 =================
            // 原版体力恢复大约是 1/11 每秒。这里额外增加 10% 的恢复速度
            if (NourishmentLevel[id] >= 3 && !player.isSprinting && player.sprintMeter < 1f)
            {
                player.sprintMeter = Mathf.Clamp(player.sprintMeter + (Time.deltaTime * 0.01f), 0f, 1f);
            }

            // ================= 2. 血之活力：体力转化与滋养积累 =================
            if (player.isSprinting)
            {
                if (player.sprintMeter <= 0.3f && !HasWarned[id])
                {
                    HasWarned[id] = true;
                    HUDManager.Instance.DisplayTip("血之活力", "体力即将耗尽！继续奔跑将抽取生命！", isWarning: true);
                }

                if (player.sprintMeter <= 0.15f && player.health > 10)
                {
                    SprintTimer[id] += Time.deltaTime;
                    if (SprintTimer[id] >= 0.25f)
                    {
                        SprintTimer[id] = 0f;

                        int damageToTake = 2;
                        if (player.health - damageToTake < 10) damageToTake = player.health - 10;

                        if (damageToTake > 0)
                        {
                            player.DamagePlayer(damageToTake, hasDamageSFX: false, callRPC: true, CauseOfDeath.Unknown, 0, false, Vector3.zero);
                            player.sprintMeter += (damageToTake * 0.02f);

                            // 累计耗血并检查是否满足滋养升级条件
                            ConsumedHealth[id] += damageToTake;
                            CheckNourishment(player, id);
                        }
                    }
                }
                else if (player.sprintMeter > 0.15f)
                {
                    SprintTimer[id] = 0f;
                }
            }
            else
            {
                SprintTimer[id] = 0f;
                if (player.sprintMeter > 0.4f)
                {
                    HasWarned[id] = false;
                }
            }
        }

        // ================= 滋养等级判定 =================
        private void CheckNourishment(PlayerControllerB player, ulong id)
        {
            int health = ConsumedHealth[id];
            int currentLevel = NourishmentLevel[id];

            if (health >= 90 && currentLevel < 3)
            {
                // 先撤销旧等级的加成，再赋予新加成，防止移速无限叠加
                if (currentLevel == 1) player.movementSpeed /= 1.1f;
                else if (currentLevel == 2) player.movementSpeed /= 1.2f;

                player.movementSpeed *= 1.3f;
                NourishmentLevel[id] = 3;
                HUDManager.Instance.DisplayTip("血之活力", "觉醒【滋养Ⅲ】：永久增加30%移速及10%体力恢复！", isWarning: false);
            }
            else if (health >= 60 && health < 90 && currentLevel < 2)
            {
                if (currentLevel == 1) player.movementSpeed /= 1.1f;

                player.movementSpeed *= 1.2f;
                NourishmentLevel[id] = 2;
                HUDManager.Instance.DisplayTip("血之活力", "觉醒【滋养Ⅱ】：永久增加20%移速！", isWarning: false);
            }
            else if (health >= 30 && health < 60 && currentLevel < 1)
            {
                player.movementSpeed *= 1.1f;
                NourishmentLevel[id] = 1;
                HUDManager.Instance.DisplayTip("血之活力", "觉醒【滋养Ⅰ】：永久增加10%移速！", isWarning: false);
            }
        }

        public override string GetStatus(ulong clientId)
        {
            if (NourishmentLevel.TryGetValue(clientId, out int lvl) && lvl > 0)
            {
                string numeral = lvl == 1 ? "Ⅰ" : (lvl == 2 ? "Ⅱ" : "Ⅲ");
                return $"<color=#FF0000>被动生效中 (滋养 {numeral} 阶)</color>";
            }
            return "<color=#FF0000>被动生效中 (燃血狂奔)</color>";
        }

        public static void ResetAll()
        {
            // 重置房间时，必须把所有玩家的移速还原
            if (StartOfRound.Instance != null)
            {
                foreach (var player in StartOfRound.Instance.allPlayerScripts)
                {
                    ulong id = player.playerClientId;
                    if (NourishmentLevel.TryGetValue(id, out int level) && player.IsOwner)
                    {
                        if (level == 1) player.movementSpeed /= 1.1f;
                        else if (level == 2) player.movementSpeed /= 1.2f;
                        else if (level == 3) player.movementSpeed /= 1.3f;
                    }
                }
            }

            HasWarned.Clear();
            LastHealth.Clear();
            SprintTimer.Clear();
            ConsumedHealth.Clear();
            NourishmentLevel.Clear();
        }
    }
}