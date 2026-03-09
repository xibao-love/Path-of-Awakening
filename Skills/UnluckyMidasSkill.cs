using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;

namespace Path_of_Awakening.Skills
{
    public class UnluckyMidasSkill : AwakeningSkill
    {
        public override string Id => "skill_unluckymidas";
        public override string Name => "【被动】厄运金手指";
        public override string Description => "首次捡起未被拿过的废品时，使其价值增加50%。代价：最大生命值永久降至20%。";

        public override void OnApply(PlayerControllerB player)
        {
            if (player.IsOwner)
            {
                if (player.health > 20)
                {
                    player.health = 20;
                    if (HUDManager.Instance != null)
                        HUDManager.Instance.UpdateHealthUI(player.health, false);
                }
            }
        }

        public override void OnRemove(PlayerControllerB player) { }

        public override void OnUpdate(PlayerControllerB player)
        {
            if (!player.IsOwner) return;

            // 强制生命值死锁：任何试图突破20的回血都会被瞬间压制
            if (player.health > 20)
            {
                player.health = 20;
                if (HUDManager.Instance != null)
                    HUDManager.Instance.UpdateHealthUI(player.health, false);
            }
        }

        // =============================================================
        // 【核心同步】：全服通用的数值修改方法（由接收器调用）
        // =============================================================
        public static void HandleMidasSync(ulong networkObjectId, int newVal)
        {
            if (NetworkManager.Singleton == null) return;

            // 顺着网线找到对应ID的物品
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
            {
                GrabbableObject obj = netObj.GetComponent<GrabbableObject>();
                if (obj != null)
                {
                    obj.SetScrapValue(newVal); // 安全统一修改
                }
            }
        }

        public override string GetStatus(ulong clientId)
        {
            return "<color=#FFD700>被动生效中 (点石成金 / 命悬一线)</color>";
        }

        public static void ResetAll()
        {
            // 旧版的防连点锁已经移除，这里留空以保持系统接口兼容
        }
    }
}