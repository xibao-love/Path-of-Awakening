using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Path_of_Awakening.Skills
{
    public class BloodlustSkill : AwakeningSkill
    {
        public override string Id => "skill_bloodlust";
        public override string Name => "【主动】浴血奋战";
        public override string Description => $"按下 [{AwakeningInputs.Instance.UseSkillKey.GetBindingDisplayString()}]开启/暂停。开启时造伤与承伤均翻倍。初始40秒，开启期间每击杀一怪延长5秒(上限80秒)。";

        public static Dictionary<ulong, bool> IsActive = new Dictionary<ulong, bool>();
        public static Dictionary<ulong, float> RemainingTime = new Dictionary<ulong, float>();

        public override void OnApply(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            IsActive[id] = false;
            RemainingTime[id] = 40f;
        }

        public override void OnRemove(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            IsActive.Remove(id);
            RemainingTime.Remove(id);
        }

        public override void OnActivate(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            if (!IsActive.ContainsKey(id)) return;

            if (RemainingTime[id] > 0)
            {
                SkillNetworkHandler.SendBloodlustAction(id, 0);
            }
            else
            {
                if (player.IsOwner) HUDManager.Instance.DisplayTip("浴血奋战", "时间已耗尽，无法开启！", isWarning: true);
            }
        }

        public static void HandleNetworkSync(ulong clientId, byte action)
        {
            if (action == 0) // 切换开关状态
            {
                if (IsActive.ContainsKey(clientId))
                {
                    IsActive[clientId] = !IsActive[clientId];
                    PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[(int)clientId];
                    if (player != null && player.IsOwner)
                    {
                        if (IsActive[clientId]) HUDManager.Instance.DisplayTip("浴血奋战", "已开启！造成伤害与承受伤害翻倍！", isWarning: true);
                        else HUDManager.Instance.DisplayTip("浴血奋战", "已暂停。", isWarning: false);
                    }
                }
            }
            else if (action == 1) // 击杀增加时间
            {
                if (RemainingTime.TryGetValue(clientId, out float time))
                {
                    RemainingTime[clientId] = Mathf.Clamp(time + 5f, 0f, 80f);
                    PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[(int)clientId];
                    if (player != null && player.IsOwner)
                    {
                        HUDManager.Instance.DisplayTip("浴血奋战", "击杀怪物，持续时间 +5 秒！", isWarning: false);
                    }
                }
            }
        }

        public override void OnUpdate(PlayerControllerB player)
        {
            if (!player.IsOwner) return;
            ulong id = player.playerClientId;
            if (!IsActive.ContainsKey(id)) return;

            if (IsActive[id])
            {
                RemainingTime[id] -= Time.deltaTime;
                if (RemainingTime[id] <= 0)
                {
                    RemainingTime[id] = 0;
                    IsActive[id] = false;
                    HUDManager.Instance.DisplayTip("浴血奋战", "时间耗尽，状态已解除！", isWarning: true);
                }
            }
        }

        public override string GetStatus(ulong clientId)
        {
            if (!IsActive.TryGetValue(clientId, out bool active) || !RemainingTime.TryGetValue(clientId, out float time)) return "";

            if (time <= 0) return "<color=#444444>已耗尽 (本局失效)</color>";
            if (active) return $"<color=#FF0000>浴血爆发中 ({time:F1}s)</color>";
            return $"<color=#00FF00>就绪/已暂停 (剩余 {time:F1}s)</color>";
        }

        public static void ResetAll()
        {
            IsActive.Clear();
            RemainingTime.Clear();
        }
    }
}