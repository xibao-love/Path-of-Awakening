using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Path_of_Awakening.Skills
{
    public class SprintBurstSkill : AwakeningSkill
    {
        public override string Id => "skill_sprintburst";
        public override string Name => "【主动】冲刺爆发";
        public override string Description => $"按下 [{AwakeningInputs.Instance.UseSkillKey.GetBindingDisplayString()}]激活。10秒内移速变为150%且无视体力。随后陷入5秒疲劳，并进入5分钟冷却。";

        // 状态记录：0=就绪, 1=爆发中, 2=疲劳中, 3=冷却中
        public Dictionary<ulong, int> State = new Dictionary<ulong, int>();
        public Dictionary<ulong, float> Timer = new Dictionary<ulong, float>();

        public override void OnApply(PlayerControllerB player)
        {
            State[player.playerClientId] = 0;
            Timer[player.playerClientId] = 0f;
        }

        public override void OnRemove(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            // 确保移除技能时如果正在加速，必须降速回来
            if (State.TryGetValue(id, out int s) && s == 1)
            {
                player.movementSpeed /= 1.5f;
            }
            State.Remove(id);
            Timer.Remove(id);
        }

        public override void OnActivate(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            if (State.TryGetValue(id, out int s) && s == 0)
            {
                State[id] = 1;
                Timer[id] = 10f;
                player.movementSpeed *= 1.5f;

                if (player.IsOwner && HUDManager.Instance != null)
                {
                    HUDManager.Instance.DisplayTip("技能激活", "冲刺爆发！", isWarning: false);
                }
            }
        }

        public override void OnUpdate(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            if (!State.ContainsKey(id)) return;

            int currentState = State[id];
            if (currentState == 0) return; // 就绪状态不耗时

            Timer[id] -= Time.deltaTime;

            if (currentState == 1) // 爆发中
            {
                player.sprintMeter = 1f; // 锁死满体力

                if (Timer[id] <= 0f)
                {
                    State[id] = 2;
                    Timer[id] = 5f;
                    player.movementSpeed /= 1.5f; // 速度复原

                    if (player.IsOwner && HUDManager.Instance != null)
                    {
                        HUDManager.Instance.DisplayTip("技能结束", "进入疲劳状态，无法奔跑！", isWarning: true);
                    }
                }
            }
            else if (currentState == 2) // 疲劳中
            {
                player.sprintMeter = 0f; // 锁死空体力
                player.isExhausted = true; // 强制大喘气

                if (Timer[id] <= 0f)
                {
                    // 【状态转换】疲劳结束，进入 5 分钟 (300秒) 的冷却期
                    State[id] = 3;
                    Timer[id] = 300f;

                    if (player.IsOwner && HUDManager.Instance != null)
                    {
                        HUDManager.Instance.DisplayTip("体力恢复", "冲刺爆发进入冷却...", isWarning: false);
                    }
                }
            }
            else if (currentState == 3) // 【新增状态】冷却中
            {
                if (Timer[id] <= 0f)
                {
                    // 冷却结束，回归就绪
                    State[id] = 0;
                    if (player.IsOwner && HUDManager.Instance != null)
                    {
                        HUDManager.Instance.DisplayTip("技能就绪", "冲刺爆发准备完毕！", isWarning: false);
                    }
                }
            }
        }

        public override string GetStatus(ulong clientId)
        {
            string keyName = AwakeningInputs.Instance.UseSkillKey.GetBindingDisplayString();
            if (!State.TryGetValue(clientId, out int s)) return "<color=#888888>未知</color>";

            switch (s)
            {
                case 0: return $"<color=#00FF00>已就绪 (按{keyName}键)</color>";
                case 1: return $"<color=#00FFFF>爆发中 ({Timer[clientId]:F1}s)</color>";
                case 2: return $"<color=#FF0000>疲劳中 ({Timer[clientId]:F1}s)</color>";
                case 3:
                    // 将秒数转化为 分:秒 格式，例如 04:59
                    int minutes = (int)(Timer[clientId] / 60);
                    int seconds = (int)(Timer[clientId] % 60);
                    return $"<color=#FFA500>冷却中 ({minutes:D2}:{seconds:D2})</color>";
                default: return "<color=#888888>未知</color>";
            }
        }
    }
}