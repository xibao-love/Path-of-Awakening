using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Path_of_Awakening.Skills
{
    public class LifeBurnHealSkill : AwakeningSkill
    {
        public override string Id => "skill_lifeburnheal";
        public override string Name => "【主动】燃命愈伤";
        public override string Description => $"按下 [{AwakeningInputs.Instance.UseSkillKey.GetBindingDisplayString()}]，每秒耗自身1%血转为队友2%血(低于20%不可用)。双方无法移动。使用后获永久虚弱：-10%移速及体力恢复。自身禁疗。";

        public static Dictionary<ulong, int> State = new Dictionary<ulong, int>();
        public static Dictionary<ulong, bool> HasDebuff = new Dictionary<ulong, bool>();
        public static Dictionary<ulong, PlayerControllerB> InteractingTarget = new Dictionary<ulong, PlayerControllerB>();

        private Dictionary<ulong, float> healTimer = new Dictionary<ulong, float>();

        public override void OnApply(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            State[id] = 0;
            HasDebuff[id] = false;
            healTimer[id] = 0f;
        }

        public override void OnRemove(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            if (State.TryGetValue(id, out int s) && s != 0) BreakHeal(id);

            if (HasDebuff.TryGetValue(id, out bool isWeak) && isWeak)
            {
                player.movementSpeed /= 0.9f;
            }

            State.Remove(id);
            HasDebuff.Remove(id);
            InteractingTarget.Remove(id);
        }

        public override void OnUpdate(PlayerControllerB player)
        {
            ulong localId = GameNetworkManager.Instance.localPlayerController.playerClientId;
            ulong id = player.playerClientId;
            if (!State.ContainsKey(id)) return;

            if (HasDebuff.TryGetValue(id, out bool isWeak) && isWeak)
            {
                if (!player.isSprinting && player.sprintMeter < 1f)
                {
                    player.sprintMeter = Mathf.Clamp(player.sprintMeter - (Time.deltaTime * 0.02f), 0f, 1f);
                }
            }

            if (!player.IsOwner) return;

            int currentState = State[id];


            if (currentState == 0)
            {
                PlayerControllerB target = GetTargetInCrosshair(player);
                if (target != null)
                {
                    // 核心排斥逻辑
                    if (SoulSymbiosisSkill.ContractPairs.ContainsKey(player.playerClientId) || SoulSymbiosisSkill.ContractPairs.ContainsKey(target.playerClientId))
                    {
                        SkillUI.Instance.CrosshairText = "<color=#FF0000>受灵魂契约排斥，无法进行血液交互</color>";
                    }
                    else if (SkillManager.ActivePlayerSkills.TryGetValue(target.playerClientId, out var tSkill) && tSkill is LifeBurnHealSkill)
                    {
                        SkillUI.Instance.CrosshairText = "<color=#FF0000>该队友体质特殊，无法被治疗</color>";
                    }
                    else
                    {
                        string skillKey = AwakeningInputs.Instance.UseSkillKey.GetBindingDisplayString();
                        SkillUI.Instance.CrosshairText = $"治疗目标: {target.playerUsername} (HP: {target.health}%)\n<size=16>[按 {skillKey} 键开始治疗]</size>";
                        // 【修改】：按键判定
                        if (AwakeningInputs.Instance.UseSkillKey.triggered)
                        {
                            if (player.health > 20) SkillNetworkHandler.SendHealAction(0, localId, target.playerClientId);
                            else HUDManager.Instance.DisplayTip("无法治疗", "自身血量不足20%！", isWarning: true);
                        }
                    }
                }
                else
                {
                    if (SkillUI.Instance.CrosshairText != "" &&
                        !SkillUI.Instance.CrosshairText.Contains("正在为你治疗") &&
                        !SkillUI.Instance.CrosshairText.Contains("正在治疗") &&
                        !SkillUI.Instance.CrosshairText.Contains("共生") &&
                        !SkillUI.Instance.CrosshairText.Contains("接受"))
                    {
                        SkillUI.Instance.CrosshairText = "";
                    }
                }
            }

            if (currentState == 1 || currentState == 2)
            {
                // 仅保留 Shift 奔跑键作为打断条件。WASD 会被底层的 disableMoveInput 拦截，按了也不会动。
                if (Keyboard.current.leftShiftKey.isPressed)
                {
                    BreakHeal(localId);
                    SkillNetworkHandler.SendHealAction(2, localId, 0);
                    return;
                }

                if (currentState == 1 && InteractingTarget.TryGetValue(id, out PlayerControllerB targetP))
                {
                    // 距离判定改为 2.5f
                    if (Vector3.Distance(player.transform.position, targetP.transform.position) > 2.5f || targetP.isPlayerDead)
                    {
                        BreakHeal(localId);
                        SkillNetworkHandler.SendHealAction(2, localId, 0);
                        return;
                    }

                    // 施法者视角：每帧实时刷新 UI 上目标的血量
                    SkillUI.Instance.CrosshairText = $"<color=#00FF00>正在治疗 {targetP.playerUsername} (HP: {targetP.health}%)</color>\n(按 Shift 奔跑可打断)";

                    healTimer[id] += Time.deltaTime;
                    if (healTimer[id] >= 1.0f)
                    {
                        healTimer[id] = 0f;
                        player.health -= 1;
                        HUDManager.Instance.UpdateHealthUI(player.health, false);

                        SkillNetworkHandler.SendHealAction(1, localId, targetP.playerClientId);

                        if (player.health <= 20)
                        {
                            HUDManager.Instance.DisplayTip("治疗停止", "血量到达底线！", isWarning: true);
                            BreakHeal(localId);
                            SkillNetworkHandler.SendHealAction(2, localId, 0);
                        }
                    }
                }
                else if (currentState == 2 && InteractingTarget.TryGetValue(id, out PlayerControllerB senderP))
                {
                    // 被治疗者视角：每帧实时刷新 UI 上自己的血量
                    SkillUI.Instance.CrosshairText = $"<color=#00FFFF>[{senderP.playerUsername} 正在为你疗伤...] (HP: {player.health}%)</color>\n(按 Shift 奔跑可打断)";
                }
            }
        }

        private PlayerControllerB GetTargetInCrosshair(PlayerControllerB player)
        {
            PlayerControllerB bestTarget = null;
            float minDistance = 2.5f;

            foreach (var p in StartOfRound.Instance.allPlayerScripts)
            {
                if (p == player || p.isPlayerDead || !p.isPlayerControlled) continue;

                float dist = Vector3.Distance(player.transform.position, p.transform.position);
                if (dist <= minDistance)
                {
                    Vector3 dirToTarget = (p.transform.position - player.gameplayCamera.transform.position).normalized;
                    if (Vector3.Angle(player.gameplayCamera.transform.forward, dirToTarget) < 25f)
                    {
                        bestTarget = p;
                        minDistance = dist;
                    }
                }
            }
            return bestTarget;
        }

        public static void BreakHeal(ulong clientId)
        {
            if (StartOfRound.Instance == null) return;
            PlayerControllerB p = StartOfRound.Instance.allPlayerScripts[clientId];

            p.disableMoveInput = false;
            if (p.IsOwner) SkillUI.Instance.CrosshairText = "";

            if (State.TryGetValue(clientId, out int s))
            {
                if (s == 1)
                {
                    if (!HasDebuff.ContainsKey(clientId) || !HasDebuff[clientId])
                    {
                        HasDebuff[clientId] = true;
                        p.movementSpeed *= 0.9f;
                        if (p.IsOwner) HUDManager.Instance.DisplayTip("燃命反噬", "你获得了虚弱状态！移速和体力恢复减慢。", isWarning: true);
                    }
                }
                State[clientId] = 0;
            }
        }

        public static void HandleNetworkMessage(byte action, ulong sender, ulong target)
        {
            if (StartOfRound.Instance == null) return;
            PlayerControllerB localP = GameNetworkManager.Instance.localPlayerController;
            ulong localId = localP.playerClientId;

            if (action == 0)
            {
                PlayerControllerB senderP = StartOfRound.Instance.allPlayerScripts[sender];
                PlayerControllerB targetP = StartOfRound.Instance.allPlayerScripts[target];

                if (localId == sender)
                {
                    State[localId] = 1;
                    InteractingTarget[localId] = targetP;
                    localP.disableMoveInput = true;
                    SkillUI.Instance.CrosshairText = $"<color=#00FF00>正在治疗 {targetP.playerUsername} (HP: {targetP.health}%)</color>\n(按 Shift 奔跑可打断)";
                }
                else if (localId == target)
                {
                    State[localId] = 2;
                    InteractingTarget[localId] = senderP;
                    localP.disableMoveInput = true;
                    SkillUI.Instance.CrosshairText = $"<color=#00FFFF>[{senderP.playerUsername} 正在为你疗伤...] (HP: {localP.health}%)</color>\n(按 Shift 奔跑可打断)";
                }
            }
            else if (action == 1)
            {
                PlayerControllerB targetP = StartOfRound.Instance.allPlayerScripts[target];
                targetP.health = Mathf.Min(targetP.health + 2, 100);

                if (localId == target)
                {
                    HUDManager.Instance.UpdateHealthUI(targetP.health, false);
                }
            }
            else if (action == 2)
            {
                if (localId == sender || (InteractingTarget.ContainsKey(localId) && InteractingTarget[localId].playerClientId == sender))
                {
                    BreakHeal(localId);
                }
            }
        }

        public override string GetStatus(ulong clientId)
        {
            string keyName = AwakeningInputs.Instance.UseSkillKey.GetBindingDisplayString();
            if (!State.TryGetValue(clientId, out int s)) return "<color=#888888>未知</color>";
            string baseStr = HasDebuff.TryGetValue(clientId, out bool weak) && weak ? "<color=#FF0000>[虚弱]</color> " : "";

            switch (s)
            {
                case 0: return baseStr + $"<color=#00FF00>就绪,按{keyName}</color>";
                case 1: return baseStr + "<color=#00FFFF>正在输血...</color>";
                case 2: return baseStr + "<color=#00FFFF>正在接受治疗</color>";
                default: return "<color=#888888>未知</color>";
            }
        }
    }
}