using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI; 

namespace Path_of_Awakening.Skills
{
    public class SoulSymbiosisSkill : AwakeningSkill
    {
        public override string Id => "skill_soulsymbiosis";
        public override string Name => "【主动】灵魂共生";
        public override string Description => $"对准队友按下 [{AwakeningInputs.Instance.UseSkillKey.GetBindingDisplayString()}]发起契约(每局限1次)。双方分摊伤害。获10%移速且禁疗。50米内显示引路连线。";

        public static Dictionary<ulong, ulong> ContractPairs = new Dictionary<ulong, ulong>();
        public static HashSet<ulong> HasContractedThisRound = new HashSet<ulong>();
        public static Dictionary<ulong, ulong> PendingProposal = new Dictionary<ulong, ulong>();

        // 【新增】：用于绘制导航线的渲染器
        public static LineRenderer SymbiosisLine;

        public override void OnApply(PlayerControllerB player) { }
        public override void OnRemove(PlayerControllerB player) { }

        public static void GlobalUpdate(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            if (!player.IsOwner) return; // 弹窗和连线只在本地计算和显示

            // 1. 处理弹窗与按键检测
            if (PendingProposal.TryGetValue(id, out ulong initiatorId))
            {
                PlayerControllerB initiator = StartOfRound.Instance.allPlayerScripts[initiatorId];
                string acceptKey = AwakeningInputs.Instance.AcceptContractKey.GetBindingDisplayString();
                string rejectKey = AwakeningInputs.Instance.RejectContractKey.GetBindingDisplayString();
                SkillUI.Instance.CrosshairText = $"<color=#FF00FF>{initiator.playerUsername} 向你发起灵魂共生！</color>\n按 [{acceptKey}] 接受 / [{rejectKey}] 拒绝";

                if (AwakeningInputs.Instance.AcceptContractKey.triggered)
                {
                    SkillNetworkHandler.SendSoulAction(1, initiatorId, id);
                    PendingProposal.Remove(id);
                    SkillUI.Instance.CrosshairText = "";
                }
                else if (AwakeningInputs.Instance.RejectContractKey.triggered)
                {
                    SkillNetworkHandler.SendSoulAction(2, initiatorId, id);
                    PendingProposal.Remove(id);
                    SkillUI.Instance.CrosshairText = "";
                }
            }

            // =============================================================
            // 2. 【新增】：绘制灵魂导航线
            // =============================================================
            if (ContractPairs.TryGetValue(id, out ulong partnerId) && StartOfRound.Instance != null)
            {
                PlayerControllerB partner = StartOfRound.Instance.allPlayerScripts[partnerId];

                // 如果双方都活着，且处于同一个空间（都在室内或都在室外）
                if (!player.isPlayerDead && !partner.isPlayerDead && player.isInsideFactory == partner.isInsideFactory)
                {
                    float dist = Vector3.Distance(player.transform.position, partner.transform.position);

                    // 距离小于 50 米时才开始绘制
                    if (dist <= 50f)
                    {
                        DrawNavigationLine(player.transform.position, partner.transform.position);
                    }
                    else if (SymbiosisLine != null) SymbiosisLine.enabled = false;
                }
                else if (SymbiosisLine != null) SymbiosisLine.enabled = false;
            }
            else if (SymbiosisLine != null) SymbiosisLine.enabled = false;
        }

        // 绘制导航线的方法
        private static void DrawNavigationLine(Vector3 start, Vector3 end)
        {
            // 如果渲染器还没创建，就实例化一个
            if (SymbiosisLine == null)
            {
                GameObject lineObj = new GameObject("SoulSymbiosisLine");
                SymbiosisLine = lineObj.AddComponent<LineRenderer>();

                // 使用最基础的着色器，防止在黑暗环境中看不见
                SymbiosisLine.material = new Material(Shader.Find("Sprites/Default"));
                SymbiosisLine.startWidth = 0.15f; // 线条粗细
                SymbiosisLine.endWidth = 0.15f;

                // 颜色设定为半透明的渐变色：从紫红色渐变到队友的青色
                SymbiosisLine.startColor = new Color(1f, 0f, 1f, 0.6f);
                SymbiosisLine.endColor = new Color(0f, 1f, 1f, 0.6f);
                SymbiosisLine.positionCount = 0;
            }

            NavMeshPath path = new NavMeshPath();
            // 尝试让游戏底层的 AI 寻路系统在你们俩之间找一条路
            if (NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
            {
                // 如果找到了合法的路线，就顺着拐角点画线
                SymbiosisLine.positionCount = path.corners.Length;
                for (int i = 0; i < path.corners.Length; i++)
                {
                    // 把线稍微抬高 0.2 米，防止陷进地板模型里被吞掉
                    SymbiosisLine.SetPosition(i, path.corners[i] + Vector3.up * 0.2f);
                }
                SymbiosisLine.enabled = true;
            }
            else
            {
                // 如果你们之间隔着无法逾越的断崖，导致系统找不到路，就暂时隐藏线条
                SymbiosisLine.enabled = false;
            }
        }

        public override void OnUpdate(PlayerControllerB player)
        {
            ulong localId = GameNetworkManager.Instance.localPlayerController.playerClientId;
            if (!player.IsOwner) return;

            if (PendingProposal.ContainsKey(localId)) return;
            if (HasContractedThisRound.Contains(localId)) return;

            PlayerControllerB target = GetTargetInCrosshair(player);
            if (target != null)
            {
                if (SkillManager.ActivePlayerSkills.TryGetValue(target.playerClientId, out var tSkill) && tSkill is SoulSymbiosisSkill)
                {
                    SkillUI.Instance.CrosshairText = "<color=#FF0000>对方也拥有灵魂共生，无法向其发起契约</color>";
                }
                else if (HasContractedThisRound.Contains(target.playerClientId))
                {
                    SkillUI.Instance.CrosshairText = "<color=#FF0000>对方已被缔结过契约</color>";
                }
                else
                {
                    string skillKey = AwakeningInputs.Instance.UseSkillKey.GetBindingDisplayString();
                    SkillUI.Instance.CrosshairText = $"灵魂目标: {target.playerUsername}\n<size=16>[按 {skillKey} 键发起契约邀请]</size>";
                    // 【修改】：按键判定
                    if (AwakeningInputs.Instance.UseSkillKey.triggered)
                    {
                        SkillNetworkHandler.SendSoulAction(0, localId, target.playerClientId);
                    }
                }
            }
            else
            {
                if (SkillUI.Instance.CrosshairText != "" &&
                    !SkillUI.Instance.CrosshairText.Contains("治疗") &&
                    !SkillUI.Instance.CrosshairText.Contains("共生") &&
                    !SkillUI.Instance.CrosshairText.Contains("接受"))
                {
                    SkillUI.Instance.CrosshairText = "";
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

        public static void HandleNetworkMessage(byte action, ulong sender, ulong target)
        {
            if (StartOfRound.Instance == null) return;
            ulong localId = GameNetworkManager.Instance.localPlayerController.playerClientId;

            if (action == 0)
            {
                if (localId == target) PendingProposal[localId] = sender;
                else if (localId == sender) HUDManager.Instance.DisplayTip("契约发送", "已向目标发送请求，等待回应...", isWarning: false);
            }
            else if (action == 1)
            {
                ContractPairs[sender] = target;
                ContractPairs[target] = sender;
                HasContractedThisRound.Add(sender);
                HasContractedThisRound.Add(target);

                if (localId == sender) StartOfRound.Instance.allPlayerScripts[sender].movementSpeed *= 1.1f;
                else if (localId == target) StartOfRound.Instance.allPlayerScripts[target].movementSpeed *= 1.1f;

                if (localId == sender || localId == target)
                {
                    PlayerControllerB partner = StartOfRound.Instance.allPlayerScripts[localId == sender ? target : sender];
                    HUDManager.Instance.DisplayTip("契约达成", $"你已与 {partner.playerUsername} 灵魂共生！", isWarning: false);
                }
            }
            else if (action == 2)
            {
                if (localId == sender) HUDManager.Instance.DisplayTip("契约被拒", "对方拒绝了你的灵魂共生请求。", isWarning: true);
            }
        }

        public static void HandleDamageMessage(ulong targetId, int damage)
        {
            if (StartOfRound.Instance == null) return;
            ulong localId = GameNetworkManager.Instance.localPlayerController.playerClientId;

            if (localId == targetId)
            {
                PlayerControllerB me = StartOfRound.Instance.allPlayerScripts[localId];
                if (!me.isPlayerDead)
                {
                    GamePatches.IsSharingDamage = true;
                    me.DamagePlayer(damage, true, true, CauseOfDeath.Unknown, 0, false, Vector3.zero);
                    GamePatches.IsSharingDamage = false;
                }
            }
        }

        public override string GetStatus(ulong clientId)
        {
            string keyName = AwakeningInputs.Instance.UseSkillKey.GetBindingDisplayString();
            if (ContractPairs.TryGetValue(clientId, out ulong partnerId) && StartOfRound.Instance != null)
                return $"<color=#FF00FF>已结契 ({StartOfRound.Instance.allPlayerScripts[partnerId].playerUsername})</color>";
            if (HasContractedThisRound.Contains(clientId)) return "<color=#888888>本局已使用</color>";
            return $"<color=#00FF00>就绪,按{keyName}</color>";
        }

        public static void ResetAll()
        {
            if (StartOfRound.Instance != null)
            {
                foreach (var player in StartOfRound.Instance.allPlayerScripts)
                {
                    if (ContractPairs.ContainsKey(player.playerClientId))
                    {
                        if (player.IsOwner) player.movementSpeed /= 1.1f;
                    }
                }
            }
            ContractPairs.Clear();
            HasContractedThisRound.Clear();
            PendingProposal.Clear();

            // 【新增】：重置房间时销毁线条对象，防止产生内存垃圾
            if (SymbiosisLine != null)
            {
                UnityEngine.Object.Destroy(SymbiosisLine.gameObject);
                SymbiosisLine = null;
            }
        }
    }
}