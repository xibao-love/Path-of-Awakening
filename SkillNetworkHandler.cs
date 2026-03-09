using Unity.Netcode;
using Unity.Collections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameNetcodeStuff;
using Path_of_Awakening.Skills;

namespace Path_of_Awakening
{
    public static class SkillNetworkHandler
    {
        public const string SyncSkillChannel = "PathOfAwakening_SyncSkill";
        public const string SyncHealChannel = "PathOfAwakening_SyncHeal";
        public const string SyncSoulChannel = "PathOfAwakening_SyncSoul";
        public const string SyncSoulDamageChannel = "PathOfAwakening_SyncSoulDamage";
        public const string SyncExplosionChannel = "PathOfAwakening_SyncExplosion";
        public const string SyncMidasChannel = "PathOfAwakening_SyncMidas"; 
        public const string SyncBloodlustChannel = "PathOfAwakening_SyncBloodlust";
        public const string SyncMagicianChannel = "PathOfAwakening_SyncMagician";


        public static void Initialize()
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.CustomMessagingManager == null) return;

            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncSkillChannel);
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncHealChannel);
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncSoulChannel);
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncSoulDamageChannel);
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncExplosionChannel);
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncMidasChannel);
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncBloodlustChannel);
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncMagicianChannel);

            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncSkillChannel, OnReceiveSkillSync);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncHealChannel, OnReceiveHealSync);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncSoulChannel, OnReceiveSoulSync);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncSoulDamageChannel, OnReceiveSoulDamageSync);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncExplosionChannel, OnReceiveExplosionSync);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncMidasChannel, OnReceiveMidasSync); 
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncBloodlustChannel, OnReceiveBloodlustSync);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncMagicianChannel, OnReceiveMagicianSync);


            Plugin.Log.LogInfo("Skill Network Handlers Successfully Registered!");
        }

        public static void SendSkillSync(ulong playerId, string skillId)
        { 
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
            using FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp);
            writer.WriteValueSafe(playerId); writer.WriteValueSafe(skillId);
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncSkillChannel, writer);
                PlayerControllerB targetPlayer = GetPlayerById(playerId);
                if (targetPlayer != null) SkillManager.ApplySkillLocally(targetPlayer, skillId);
            }
            else NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncSkillChannel, NetworkManager.ServerClientId, writer);
        }
        private static void OnReceiveSkillSync(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ulong playerId); reader.ReadValueSafe(out string skillId);
            if (NetworkManager.Singleton.IsServer && senderId != NetworkManager.ServerClientId)
            {
                using FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp);
                writer.WriteValueSafe(playerId); writer.WriteValueSafe(skillId);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncSkillChannel, writer);
                PlayerControllerB targetPlayer = GetPlayerById(playerId);
                if (targetPlayer != null) SkillManager.ApplySkillLocally(targetPlayer, skillId);
            }
            else if (!NetworkManager.Singleton.IsServer)
            {
                PlayerControllerB targetPlayer = GetPlayerById(playerId);
                if (targetPlayer != null) SkillManager.ApplySkillLocally(targetPlayer, skillId);
            }
        }

        public static void SendHealAction(byte action, ulong sender, ulong target)
        { 
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
            using FastBufferWriter writer = new FastBufferWriter(24, Allocator.Temp);
            writer.WriteValueSafe(action); writer.WriteValueSafe(sender); writer.WriteValueSafe(target);
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncHealChannel, writer);
                LifeBurnHealSkill.HandleNetworkMessage(action, sender, target);
            }
            else NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncHealChannel, NetworkManager.ServerClientId, writer);
        }
        private static void OnReceiveHealSync(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte action); reader.ReadValueSafe(out ulong sender); reader.ReadValueSafe(out ulong target);
            if (NetworkManager.Singleton.IsServer && senderId != NetworkManager.ServerClientId)
            {
                using FastBufferWriter writer = new FastBufferWriter(24, Allocator.Temp);
                writer.WriteValueSafe(action); writer.WriteValueSafe(sender); writer.WriteValueSafe(target);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncHealChannel, writer);
                LifeBurnHealSkill.HandleNetworkMessage(action, sender, target);
            }
            else if (!NetworkManager.Singleton.IsServer) LifeBurnHealSkill.HandleNetworkMessage(action, sender, target);
        }

        public static void SendSoulAction(byte action, ulong sender, ulong target)
        { 
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
            using FastBufferWriter writer = new FastBufferWriter(24, Allocator.Temp);
            writer.WriteValueSafe(action); writer.WriteValueSafe(sender); writer.WriteValueSafe(target);
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncSoulChannel, writer);
                SoulSymbiosisSkill.HandleNetworkMessage(action, sender, target);
            }
            else NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncSoulChannel, NetworkManager.ServerClientId, writer);
        }
        private static void OnReceiveSoulSync(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte action); reader.ReadValueSafe(out ulong sender); reader.ReadValueSafe(out ulong target);
            if (NetworkManager.Singleton.IsServer && senderId != NetworkManager.ServerClientId)
            {
                using FastBufferWriter writer = new FastBufferWriter(24, Allocator.Temp);
                writer.WriteValueSafe(action); writer.WriteValueSafe(sender); writer.WriteValueSafe(target);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncSoulChannel, writer);
                SoulSymbiosisSkill.HandleNetworkMessage(action, sender, target);
            }
            else if (!NetworkManager.Singleton.IsServer) SoulSymbiosisSkill.HandleNetworkMessage(action, sender, target);
        }

        public static void SendSoulDamageAction(ulong target, int damage)
        { 
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
            using FastBufferWriter writer = new FastBufferWriter(12, Allocator.Temp);
            writer.WriteValueSafe(target); writer.WriteValueSafe(damage);
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncSoulDamageChannel, writer);
                SoulSymbiosisSkill.HandleDamageMessage(target, damage);
            }
            else NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncSoulDamageChannel, NetworkManager.ServerClientId, writer);
        }
        private static void OnReceiveSoulDamageSync(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ulong target); reader.ReadValueSafe(out int damage);
            if (NetworkManager.Singleton.IsServer && senderId != NetworkManager.ServerClientId)
            {
                using FastBufferWriter writer = new FastBufferWriter(12, Allocator.Temp);
                writer.WriteValueSafe(target); writer.WriteValueSafe(damage);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncSoulDamageChannel, writer);
                SoulSymbiosisSkill.HandleDamageMessage(target, damage);
            }
            else if (!NetworkManager.Singleton.IsServer) SoulSymbiosisSkill.HandleDamageMessage(target, damage);
        }

        public static void SendExplosionAction(UnityEngine.Vector3 pos)
        { 
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
            using FastBufferWriter writer = new FastBufferWriter(12, Allocator.Temp);
            writer.WriteValueSafe(pos.x); writer.WriteValueSafe(pos.y); writer.WriteValueSafe(pos.z);
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncExplosionChannel, writer);
                DeadResentmentSkill.HandleExplosion(pos);
            }
            else NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncExplosionChannel, NetworkManager.ServerClientId, writer);
        }
        private static void OnReceiveExplosionSync(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out float x); reader.ReadValueSafe(out float y); reader.ReadValueSafe(out float z);
            UnityEngine.Vector3 pos = new UnityEngine.Vector3(x, y, z);
            if (NetworkManager.Singleton.IsServer && senderId != NetworkManager.ServerClientId)
            {
                using FastBufferWriter writer = new FastBufferWriter(12, Allocator.Temp);
                writer.WriteValueSafe(x); writer.WriteValueSafe(y); writer.WriteValueSafe(z);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncExplosionChannel, writer);
                DeadResentmentSkill.HandleExplosion(pos);
            }
            else if (!NetworkManager.Singleton.IsServer) DeadResentmentSkill.HandleExplosion(pos);
        }

        // ================= 【新增】厄运金手指数据同步 =================
        public static void SendMidasAction(ulong networkObjectId, int newVal)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
            using FastBufferWriter writer = new FastBufferWriter(12, Allocator.Temp);
            writer.WriteValueSafe(networkObjectId);
            writer.WriteValueSafe(newVal);

            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncMidasChannel, writer);
                UnluckyMidasSkill.HandleMidasSync(networkObjectId, newVal);
            }
            else
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncMidasChannel, NetworkManager.ServerClientId, writer);
            }
        }

        private static void OnReceiveMidasSync(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ulong networkObjectId);
            reader.ReadValueSafe(out int newVal);

            if (NetworkManager.Singleton.IsServer && senderId != NetworkManager.ServerClientId)
            {
                using FastBufferWriter writer = new FastBufferWriter(12, Allocator.Temp);
                writer.WriteValueSafe(networkObjectId);
                writer.WriteValueSafe(newVal);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncMidasChannel, writer);
                UnluckyMidasSkill.HandleMidasSync(networkObjectId, newVal);
            }
            else if (!NetworkManager.Singleton.IsServer)
            {
                UnluckyMidasSkill.HandleMidasSync(networkObjectId, newVal);
            }
        }

        private static PlayerControllerB GetPlayerById(ulong clientId)
        {
            if (StartOfRound.Instance == null) return null;
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
                if (player.playerClientId == clientId) return player;
            return null;
        }

        // ================= 【新增】浴血奋战网络同步 =================
        public static void SendBloodlustAction(ulong targetClient, byte action)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
            using FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);
            writer.WriteValueSafe(targetClient);
            writer.WriteValueSafe(action); // action 0: 切换状态, 1: 击杀加时间

            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncBloodlustChannel, writer);
                BloodlustSkill.HandleNetworkSync(targetClient, action);
            }
            else
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncBloodlustChannel, NetworkManager.ServerClientId, writer);
            }
        }

        private static void OnReceiveBloodlustSync(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ulong targetClient);
            reader.ReadValueSafe(out byte action);

            if (NetworkManager.Singleton.IsServer && senderId != NetworkManager.ServerClientId)
            {
                using FastBufferWriter writer = new FastBufferWriter(9, Allocator.Temp);
                writer.WriteValueSafe(targetClient);
                writer.WriteValueSafe(action);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncBloodlustChannel, writer);
                BloodlustSkill.HandleNetworkSync(targetClient, action);
            }
            else if (!NetworkManager.Singleton.IsServer)
            {
                BloodlustSkill.HandleNetworkSync(targetClient, action);
            }
        }

        // ================= 【新增】魔术师技能同步 =================
        public static void SendMagicianAction(byte action, ulong playerId, ulong objectId = 0)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
            using FastBufferWriter writer = new FastBufferWriter(17, Allocator.Temp);
            writer.WriteValueSafe(action);
            writer.WriteValueSafe(playerId);
            writer.WriteValueSafe(objectId);

            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncMagicianChannel, writer);
                if (action == 0 || action == 1) MagicianSkill.HandleServerRequest(action, playerId, objectId);
                else MagicianSkill.HandleNetworkSync(action, playerId, objectId);
            }
            else
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncMagicianChannel, NetworkManager.ServerClientId, writer);
            }
        }

        private static void OnReceiveMagicianSync(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte action);
            reader.ReadValueSafe(out ulong playerId);
            reader.ReadValueSafe(out ulong objectId);

            if (NetworkManager.Singleton.IsServer && senderId != NetworkManager.ServerClientId)
            {
                if (action == 0 || action == 1) MagicianSkill.HandleServerRequest(action, playerId, objectId);
            }
            else if (!NetworkManager.Singleton.IsServer)
            {
                MagicianSkill.HandleNetworkSync(action, playerId, objectId);
            }
        }

    }
}