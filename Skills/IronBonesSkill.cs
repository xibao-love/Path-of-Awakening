using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Path_of_Awakening.Skills
{
    public class IronBonesSkill : AwakeningSkill
    {
        public override string Id => "skill_ironbones";
        public override string Name => "【主动】钢筋铁骨";
        public override string Description => $"按下 [{AwakeningInputs.Instance.UseSkillKey.GetBindingDisplayString()}]在0.5秒内无敌。成功抵挡伤害获10秒130%移速且无视体力，随后疲劳5秒，此后失效。";

        // 状态机: 0=就绪, 1=招架中(0.5s), 2=爆发中(10s), 3=疲劳中(5s), 4=已失效, 5=冷却中(招架失败)
        public static Dictionary<ulong, int> State = new Dictionary<ulong, int>();
        public static Dictionary<ulong, float> Timers = new Dictionary<ulong, float>();

        public override void OnApply(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            State[id] = 0;
            Timers[id] = 0f;
        }

        public override void OnRemove(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            if (State.TryGetValue(id, out int s) && s == 2)
            {
                player.movementSpeed /= 1.3f; // 归还移速
            }
            State.Remove(id);
            Timers.Remove(id);
        }

        public override void OnActivate(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            if (State.TryGetValue(id, out int s) && s == 0)
            {
                State[id] = 1;
                Timers[id] = 0.5f;
                if (player.IsOwner) HUDManager.Instance.DisplayTip("钢筋铁骨", "进入无敌判定！(0.5秒)", isWarning: false);
            }
        }

        public override void OnUpdate(PlayerControllerB player)
        {
            if (!player.IsOwner) return;
            ulong id = player.playerClientId;
            if (!State.ContainsKey(id)) return;

            int currentState = State[id];

            if (currentState == 1) // 招架判定中
            {
                Timers[id] -= Time.deltaTime;
                if (Timers[id] <= 0)
                {
                    State[id] = 5;
                    Timers[id] = 60f;
                }
            }
            else if (currentState == 2) // 招架成功，爆发中
            {
                player.sprintMeter = 1f; // 锁定满体力
                player.isExhausted = false; // 移除所有喘气疲劳

                Timers[id] -= Time.deltaTime;
                if (Timers[id] <= 0)
                {
                    State[id] = 3; // 转入惩罚状态
                    Timers[id] = 5f;
                    player.movementSpeed /= 1.3f; // 撤销移速加成
                    HUDManager.Instance.DisplayTip("钢筋铁骨", "爆发结束，进入力竭疲劳状态！", isWarning: true);
                }
            }
            else if (currentState == 3) // 疲劳惩罚中
            {
                player.sprintMeter = 0f; // 锁定空体力
                player.isExhausted = true; // 强制大喘气，无法奔跑

                Timers[id] -= Time.deltaTime;
                if (Timers[id] <= 0)
                {
                    State[id] = 4; // 技能彻底失效
                    HUDManager.Instance.DisplayTip("钢筋铁骨", "体力恢复，但装备已损坏(本局失效)。", isWarning: false);
                }
            }
            else if (currentState == 5) // 短冷却中
            {
                Timers[id] -= Time.deltaTime;
                if (Timers[id] <= 0)
                {
                    State[id] = 0; // 重新就绪
                }
            }
        }

        // 被 GamePatches 拦截伤害时调用
        public static void TriggerParry(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            if (State.TryGetValue(id, out int s) && s == 1)
            {
                State[id] = 2; // 转入爆发状态
                Timers[id] = 10f;
                player.movementSpeed *= 1.3f; // +30% 移速
                if (player.IsOwner) HUDManager.Instance.DisplayTip("招架成功！", "完美抵挡伤害！移速暴增，无限体力！", isWarning: false);
            }
        }

        public override string GetStatus(ulong clientId)
        {
            string keyName = AwakeningInputs.Instance.UseSkillKey.GetBindingDisplayString();
            if (!State.TryGetValue(clientId, out int s)) return "";
            switch (s)
            {
                case 0: return $"<color=#00FF00>就绪,按{keyName}</color>";
                case 1: return "<color=#FFFF00>无敌判定中...</color>";
                case 2: return $"<color=#FF0000>钢筋铁骨爆发中 ({Timers[clientId]:F1}s)</color>";
                case 3: return $"<color=#888888>极度疲劳中 ({Timers[clientId]:F1}s)</color>";
                case 4: return "<color=#444444>已毁坏 (本局失效)</color>";
                case 5: return $"<color=#FFFF00>冷却中 ({Timers[clientId]:F1}s)</color>";
                default: return "";
            }
        }

        public static void ResetAll()
        {
            if (StartOfRound.Instance != null)
            {
                foreach (var player in StartOfRound.Instance.allPlayerScripts)
                {
                    ulong id = player.playerClientId;
                    if (State.TryGetValue(id, out int s) && s == 2 && player.IsOwner)
                    {
                        player.movementSpeed /= 1.3f; // 防止退房时移速残留
                    }
                }
            }
            State.Clear();
            Timers.Clear();
        }
    }
}