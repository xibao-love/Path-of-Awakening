using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Path_of_Awakening.Skills;
using Unity.Netcode;

namespace Path_of_Awakening
{
    public static class SkillManager
    {
        public static Dictionary<string, AwakeningSkill> SkillRegistry = new Dictionary<string, AwakeningSkill>();
        public static Dictionary<ulong, AwakeningSkill> ActivePlayerSkills = new Dictionary<ulong, AwakeningSkill>();

        public static void Initialize()
        {
            RegisterSkill(new MingDaoSkill());
            RegisterSkill(new SprintBurstSkill());
            RegisterSkill(new SmoothLandingSkill());
            RegisterSkill(new LifeBurnHealSkill());
            RegisterSkill(new SoulSymbiosisSkill());
            RegisterSkill(new DeadResentmentSkill());
            RegisterSkill(new BloodVitalitySkill());
            RegisterSkill(new UnluckyMidasSkill());
            RegisterSkill(new IronBonesSkill());
            RegisterSkill(new BloodlustSkill());
            RegisterSkill(new MagicianSkill());
        }

        public static void RegisterSkill(AwakeningSkill skill)
        {
            if (!SkillRegistry.ContainsKey(skill.Id))
            {
                SkillRegistry.Add(skill.Id, skill);
                Plugin.Log.LogInfo($"Registered skill: {skill.Name}");
            }
        }

        public static void RequestSkill(PlayerControllerB player, string skillId)
        {
            if (player == null || !SkillRegistry.ContainsKey(skillId)) return;
            if (ActivePlayerSkills.ContainsKey(player.playerClientId)) return;
            SkillNetworkHandler.SendSkillSync(player.playerClientId, skillId);
        }

        public static void ApplySkillLocally(PlayerControllerB player, string skillId)
        {
            if (player == null || !SkillRegistry.TryGetValue(skillId, out var skill)) return;
            ulong playerId = player.playerClientId;

            if (ActivePlayerSkills.TryGetValue(playerId, out var oldSkill))
            {
                oldSkill.OnRemove(player);
                ActivePlayerSkills.Remove(playerId);
            }

            ActivePlayerSkills.Add(playerId, skill);
            skill.OnApply(player);
            Plugin.Log.LogInfo($"[Network Sync] Player {player.playerUsername} equipped skill: {skill.Name}");
        }

        public static void AutoAssignSkills()
        {
            if (StartOfRound.Instance == null || !NetworkManager.Singleton.IsServer) return;
            var availableSkills = SkillRegistry.Values.ToList();
            if (availableSkills.Count == 0) return;

            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.isPlayerControlled || player.isPlayerDead)
                {
                    if (!ActivePlayerSkills.ContainsKey(player.playerClientId))
                    {
                        var randomSkill = availableSkills[UnityEngine.Random.Range(0, availableSkills.Count)];
                        SkillNetworkHandler.SendSkillSync(player.playerClientId, randomSkill.Id);
                    }
                }
            }
        }

        public static void ClearSkills()
        {
            if (StartOfRound.Instance == null) return;
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (ActivePlayerSkills.TryGetValue(player.playerClientId, out var skill))
                {
                    skill.OnRemove(player);
                }
            }
            ActivePlayerSkills.Clear();

            SoulSymbiosisSkill.ResetAll();
            BloodVitalitySkill.ResetAll();
            UnluckyMidasSkill.ResetAll();
            IronBonesSkill.ResetAll();
            MingDaoSkill.ResetAll();
            BloodlustSkill.ResetAll();
            MagicianSkill.ResetAll();

            LifeBurnHealSkill.State.Clear();
            LifeBurnHealSkill.HasDebuff.Clear();
            LifeBurnHealSkill.InteractingTarget.Clear();

            Plugin.Log.LogInfo("All skills and static states have been cleared.");
        }

        public static void CompletelyResetSkills()
        {
            ActivePlayerSkills.Clear();

            SkillRegistry.Clear();
            Initialize();

            LifeBurnHealSkill.State.Clear();
            LifeBurnHealSkill.HasDebuff.Clear();
            LifeBurnHealSkill.InteractingTarget.Clear();

            SoulSymbiosisSkill.ContractPairs.Clear();
            SoulSymbiosisSkill.HasContractedThisRound.Clear();
            SoulSymbiosisSkill.PendingProposal.Clear();

            BloodVitalitySkill.ResetAll();
            UnluckyMidasSkill.ResetAll();
            IronBonesSkill.ResetAll();
            MingDaoSkill.ResetAll();
            BloodlustSkill.ResetAll();
            MagicianSkill.ResetAll();

            if (SkillUI.Instance != null)
            {
                SkillUI.Instance.CrosshairText = "";
                SkillUI.Instance.SetPanelState(false);
            }

            Plugin.Log.LogInfo("退出房间：已彻底清空并重置所有跨局技能状态！");
        }
    }
}