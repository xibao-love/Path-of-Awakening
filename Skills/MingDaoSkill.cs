using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;

namespace Path_of_Awakening.Skills
{
    public class MingDaoSkill : AwakeningSkill
    {
        public override string Id => "skill_mingdao";
        public override string Name => "【被动】名刀司命";
        public override string Description => "抵挡致命伤害保留1点血量。触发后获得5秒150%移速，随后疲劳2秒。本局限1次。";

        // 兼容原有的触发判定
        public Dictionary<ulong, bool> HasTriggered = new Dictionary<ulong, bool>();

        // 状态机: 0=就绪, 1=加速爆发中(5s), 2=疲劳脱力中(2s), 3=已失效
        public static Dictionary<ulong, int> State = new Dictionary<ulong, int>();
        public static Dictionary<ulong, float> Timers = new Dictionary<ulong, float>();

        public override void OnApply(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            HasTriggered[id] = false;
            State[id] = 0;
            Timers[id] = 0f;
        }

        public override void OnRemove(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            if (State.TryGetValue(id, out int s) && s == 1)
            {
                player.movementSpeed /= 1.5f; // 【修改】如果刚好在爆发期间退房，必须归还 1.5 倍移速
            }
            HasTriggered.Remove(id);
            State.Remove(id);
            Timers.Remove(id);
        }

        public override void OnUpdate(PlayerControllerB player)
        {
            if (!player.IsOwner) return;
            ulong id = player.playerClientId;
            if (!State.ContainsKey(id)) return;

            int currentState = State[id];

            if (currentState == 1) // 爆发加速中
            {
                Timers[id] -= Time.deltaTime;
                if (Timers[id] <= 0)
                {
                    State[id] = 2; // 转入惩罚状态
                    Timers[id] = 2f;
                    player.movementSpeed /= 1.5f; // 【修改】撤销 1.5 倍移速加成
                    HUDManager.Instance.DisplayTip("名刀司命", "加速结束，进入虚弱疲劳状态！", isWarning: true);
                }
            }
            else if (currentState == 2) // 疲劳虚弱中
            {
                player.sprintMeter = 0f; // 锁定空体力
                player.isExhausted = true; // 强制大喘气，无法奔跑

                Timers[id] -= Time.deltaTime;
                if (Timers[id] <= 0)
                {
                    State[id] = 3; // 彻底失效
                }
            }
        }

        // 被 GamePatches 拦截致死伤害时调用
        public static void TriggerEffect(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            State[id] = 1;
            Timers[id] = 5f;
            player.movementSpeed *= 1.5f; // 【修改】基础移速 x 1.5 倍
        }

        public override string GetStatus(ulong clientId)
        {
            if (!State.TryGetValue(clientId, out int s)) return "";
            switch (s)
            {
                case 0: return "<color=#00FF00>就绪 (等待致死打击)</color>";
                case 1: return $"<color=#FF0000>名刀爆发中 ({Timers[clientId]:F1}s)</color>";
                case 2: return $"<color=#888888>脱力疲劳中 ({Timers[clientId]:F1}s)</color>";
                case 3: return "<color=#444444>已碎裂 (本局失效)</color>";
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
                    if (State.TryGetValue(id, out int s) && s == 1 && player.IsOwner)
                    {
                        player.movementSpeed /= 1.5f; // 【修改】防止退房时移速残留
                    }
                }
            }
            State.Clear();
            Timers.Clear();
        }
    }
}