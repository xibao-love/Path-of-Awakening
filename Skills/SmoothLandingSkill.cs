using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;

namespace Path_of_Awakening.Skills
{
    public class SmoothLandingSkill : AwakeningSkill
    {
        public override string Id => "skill_smoothlanding";
        public override string Name => "【被动】平稳着陆";
        public override string Description => "从高于5米处落地后，获得5秒150%移速且无视体力。随后虚弱3秒，冷却3分钟。";

        // 状态记录：0=就绪, 1=爆发中, 2=疲劳中, 3=冷却中
        public Dictionary<ulong, int> State = new Dictionary<ulong, int>();
        public Dictionary<ulong, float> Timer = new Dictionary<ulong, float>();

        // 核心：用于计算真实掉落高度的追踪变量
        private Dictionary<ulong, float> highestY = new Dictionary<ulong, float>();
        private Dictionary<ulong, bool> wasGrounded = new Dictionary<ulong, bool>();

        public override void OnApply(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            State[id] = 0;
            Timer[id] = 0f;

            // 初始化高度追踪
            highestY[id] = player.transform.position.y;
            wasGrounded[id] = true;
        }

        public override void OnRemove(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            if (State.TryGetValue(id, out int s) && s == 1)
            {
                player.movementSpeed /= 1.5f; // 防止技能被没收时移速卡在150%
            }
            State.Remove(id);
            Timer.Remove(id);
            highestY.Remove(id);
            wasGrounded.Remove(id);
        }

        public override void OnUpdate(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            if (!State.ContainsKey(id)) return;

            // ================= 1. 坠落高度检测逻辑 =================
            if (!wasGrounded.ContainsKey(id)) wasGrounded[id] = true;
            if (!highestY.ContainsKey(id)) highestY[id] = player.transform.position.y;

            // 获取游戏自带的角色控制器接地状态
            bool isGrounded = player.thisController.isGrounded;

            if (!isGrounded)
            {
                // 如果在空中（跳跃、下落），不断刷新记录玩家达到的最高点
                if (player.transform.position.y > highestY[id])
                {
                    highestY[id] = player.transform.position.y;
                }
            }
            else
            {
                // 刚刚双脚着地的这一瞬间 (上一帧在空中，这一帧在地上)
                if (!wasGrounded[id])
                {
                    // 计算总掉落高度 (Unity中1个单位=1米)
                    float fallDistance = highestY[id] - player.transform.position.y;

                    // 判定：掉落高度 >= 5米，且没有发生瞬移（大于50米一般是进出大门切图）
                    if (fallDistance >= 5f && fallDistance <= 50f && State[id] == 0)
                    {
                        // 触发被动
                        State[id] = 1;
                        Timer[id] = 5f;
                        player.movementSpeed *= 1.5f;

                        if (player.IsOwner && HUDManager.Instance != null)
                        {
                            HUDManager.Instance.DisplayTip("被动触发", "平稳着陆：加速爆发！", isWarning: false);
                        }
                    }
                }
                // 在地面上时，不断更新最高点为当前高度，作为下一次跳跃的起点
                highestY[id] = player.transform.position.y;
            }
            wasGrounded[id] = isGrounded; // 保存本帧接地状态供下一帧对比

            // ================= 2. 状态机逻辑 =================
            int currentState = State[id];
            if (currentState == 0) return;

            Timer[id] -= Time.deltaTime;

            if (currentState == 1) // 爆发中
            {
                player.sprintMeter = 1f; // 锁死满体力
                if (Timer[id] <= 0f)
                {
                    State[id] = 2;
                    Timer[id] = 3f; // 虚弱3秒
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
                    State[id] = 3;
                    Timer[id] = 180f; // 3分钟 = 180秒冷却

                    if (player.IsOwner && HUDManager.Instance != null)
                    {
                        HUDManager.Instance.DisplayTip("体力恢复", "平稳着陆进入冷却...", isWarning: false);
                    }
                }
            }
            else if (currentState == 3) // 冷却中
            {
                if (Timer[id] <= 0f)
                {
                    State[id] = 0;
                    if (player.IsOwner && HUDManager.Instance != null)
                    {
                        HUDManager.Instance.DisplayTip("技能就绪", "平稳着陆准备完毕！", isWarning: false);
                    }
                }
            }
        }

        public override string GetStatus(ulong clientId)
        {
            if (!State.TryGetValue(clientId, out int s)) return "<color=#888888>未知</color>";

            switch (s)
            {
                case 0: return "<color=#00FF00>已就绪 (跌落触发)</color>";
                case 1: return $"<color=#00FFFF>加速中 ({Timer[clientId]:F1}s)</color>";
                case 2: return $"<color=#FF0000>疲劳中 ({Timer[clientId]:F1}s)</color>";
                case 3:
                    int minutes = (int)(Timer[clientId] / 60);
                    int seconds = (int)(Timer[clientId] % 60);
                    return $"<color=#FFA500>冷却中 ({minutes:D2}:{seconds:D2})</color>";
                default: return "<color=#888888>未知</color>";
            }
        }
    }
}