using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using UnityEngine.InputSystem;

namespace Path_of_Awakening.Skills
{
    public class MagicianSkill : AwakeningSkill
    {
        public override string Id => "skill_magician";
        public override string Name => "【主动】魔术师";
        public override string Description => $"按下 [{AwakeningInputs.Instance.UseSkillKey.GetBindingDisplayString()}] 在手上生成一把不可丢弃的专属铲子，再次按下消失。死亡后消失。无法使用其它武器。";

        public static Dictionary<ulong, ulong> PlayerToShovel = new Dictionary<ulong, ulong>();
        public static Dictionary<ulong, float> CooldownTimer = new Dictionary<ulong, float>();

        public override void OnApply(PlayerControllerB player)
        {
            CooldownTimer[player.playerClientId] = 0f;
        }

        public override void OnRemove(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            if (PlayerToShovel.ContainsKey(id) && player.IsOwner)
            {
                SkillNetworkHandler.SendMagicianAction(1, id);
            }
            CooldownTimer.Remove(id);
            PlayerToShovel.Remove(id);
        }

        public override void OnActivate(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            if (CooldownTimer.TryGetValue(id, out float timer) && timer > 0) return;

            CooldownTimer[id] = 1f; // 1秒防狂按冷却

            if (PlayerToShovel.ContainsKey(id))
            {
                Plugin.Log.LogInfo("[MagicianSkill] 玩家请求收回铲子");
                SkillNetworkHandler.SendMagicianAction(1, id);
            }
            else
            {
                Plugin.Log.LogInfo("[MagicianSkill] 玩家请求生成专属铲子");
                if (player.currentlyHeldObjectServer != null)
                {
                    Vector3 dropPosition = player.transform.position + Vector3.up * 0.25f;

                    NetworkObject dropParent = player.isInElevator ? StartOfRound.Instance.elevatorTransform.gameObject.GetComponent<NetworkObject>() : null;

                    player.DiscardHeldObject(placeObject: false, parentObjectTo: dropParent, placePosition: dropPosition);
                }
                SkillNetworkHandler.SendMagicianAction(0, id);
            }
        }

        public override void OnUpdate(PlayerControllerB player)
        {
            ulong id = player.playerClientId;
            if (CooldownTimer.ContainsKey(id) && CooldownTimer[id] > 0)
            {
                CooldownTimer[id] -= Time.deltaTime;
            }
        }

        // =========================================================
        // 网络同步核心枢纽
        // =========================================================
        public static void HandleServerRequest(byte action, ulong playerId, ulong objectId)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if (action == 0) // 生成请求
            {
                if (PlayerToShovel.ContainsKey(playerId)) return;

                PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[(int)playerId];

                // 直接找预制体名字（无视任何汉化Mod），并兼容中文名
                Item shovelItem = StartOfRound.Instance.allItemsList.itemsList.FirstOrDefault(i =>
                    (i.spawnPrefab != null && i.spawnPrefab.name.ToLower().Contains("shovel")) ||
                    (i.itemName != null && (i.itemName.ToLower().Contains("shovel") || i.itemName.Contains("铲")))
                );

                // 防御性编程：如果总物品库里被其他Mod弄丢了，就去终端机商店列表里捞
                if (shovelItem == null)
                {
                    Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
                    if (terminal != null)
                    {
                        shovelItem = terminal.buyableItemsList?.FirstOrDefault(i =>
                            (i.spawnPrefab != null && i.spawnPrefab.name.ToLower().Contains("shovel")) ||
                            (i.itemName != null && (i.itemName.ToLower().Contains("shovel") || i.itemName.Contains("铲")))
                        );
                    }
                }

                if (shovelItem != null)
                {
                    Plugin.Log.LogInfo($"[MagicianSkill] 成功找到铲子预制体，正在为 {player.playerUsername} 具现化...");

                    GameObject shovelObj = UnityEngine.Object.Instantiate(
                        shovelItem.spawnPrefab,
                        player.transform.position + Vector3.up * 1.5f,
                        Quaternion.identity
                    );

                    GrabbableObject grabObj = shovelObj.GetComponent<GrabbableObject>();
                    grabObj.EnablePhysics(false);
                    grabObj.fallTime = 1f;
                    grabObj.SetScrapValue(0);

                    NetworkObject netObj = shovelObj.GetComponent<NetworkObject>();
                    netObj.Spawn(destroyWithScene: true); // 安全生成

                    Plugin.Log.LogInfo($"[MagicianSkill] 魔术铲生成成功！网络ID: {netObj.NetworkObjectId}");
                    SkillNetworkHandler.SendMagicianAction(2, playerId, netObj.NetworkObjectId);
                }
                else
                {
                    Plugin.Log.LogError("[MagicianSkill] 致命错误：在物品库中找不到铲子(Shovel)！");
                }
            }
            else if (action == 1) // 销毁请求
            {
                if (PlayerToShovel.TryGetValue(playerId, out ulong magicShovelId))
                {
                    if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(magicShovelId, out NetworkObject netObj))
                    {
                        netObj.Despawn();
                    }
                    SkillNetworkHandler.SendMagicianAction(3, playerId, magicShovelId);
                }
            }
        }

        public static void HandleNetworkSync(byte action, ulong playerId, ulong objectId)
        {
            if (action == 2) // 铲子已生成
            {
                PlayerToShovel[playerId] = objectId;
                GameNetworkManager.Instance.StartCoroutine(GrabShovelRoutine(playerId, objectId));
            }
            else if (action == 3) // 铲子已销毁
            {
                PlayerToShovel.Remove(playerId);

                PlayerControllerB targetPlayer = StartOfRound.Instance.allPlayerScripts[(int)playerId];
                if (targetPlayer != null)
                {
                    // 遍历玩家所有的物品槽进行判定
                    for (int i = 0; i < targetPlayer.ItemSlots.Length; i++)
                    {
                        GrabbableObject item = targetPlayer.ItemSlots[i];
                        bool shouldClear = false;

                        if (item == null)
                        {
                            shouldClear = true; 
                        }
                        else
                        {
                            // 通过网络 ID 精准匹配。即使引擎还没来得及销毁模型，我们也强制清理它所在的 UI 槽位
                            try { if (item.NetworkObjectId == objectId) shouldClear = true; }
                            catch { shouldClear = true; } // 防止对象在访问瞬间被销毁报错
                        }

                        if (shouldClear)
                        {
                            targetPlayer.ItemSlots[i] = null;
                            if (targetPlayer.IsOwner)
                            {
                                HUDManager.Instance.itemSlotIcons[i].enabled = false;
                                HUDManager.Instance.itemSlotIcons[i].sprite = null;
                            }
                        }
                    }

                    // 重新判定当前手持物品状态
                    targetPlayer.currentlyHeldObjectServer = targetPlayer.ItemSlots[targetPlayer.currentItemSlot];
                    targetPlayer.isHoldingObject = targetPlayer.currentlyHeldObjectServer != null;

                    if (!targetPlayer.isHoldingObject)
                    {
                        targetPlayer.playerBodyAnimator.SetBool("Grab", false);
                        targetPlayer.twoHanded = false;

                        Transform holder = targetPlayer.IsOwner ? targetPlayer.localItemHolder : targetPlayer.serverItemHolder;
                        if (holder != null)
                        {
                            foreach (Transform child in holder)
                            {
                                UnityEngine.Object.Destroy(child.gameObject);
                            }
                        }
                    }
                    else
                    {
                        targetPlayer.twoHanded = targetPlayer.currentlyHeldObjectServer.itemProperties.twoHanded;
                    }
                }
            }
        }

        private static System.Collections.IEnumerator GrabShovelRoutine(ulong playerId, ulong objectId)
        {
            Plugin.Log.LogInfo($"[MagicianSkill] 客机正在等待网络物品 {objectId} 同步...");
            float timeout = 5f;
            while (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(objectId) && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (timeout <= 0)
            {
                Plugin.Log.LogError("[MagicianSkill] 超时！魔术铲未同步到客机。");
                yield break;
            }

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
            {
                GrabbableObject shovel = netObj.GetComponent<GrabbableObject>();
                PlayerControllerB targetPlayer = StartOfRound.Instance.allPlayerScripts[(int)playerId];

                if (shovel != null && targetPlayer != null)
                {
                    Plugin.Log.LogInfo($"[MagicianSkill] 开始执行底层强制装备: {targetPlayer.playerUsername}");
                    ForceEquipItem(targetPlayer, shovel);
                }
            }
        }

        private static void ForceEquipItem(PlayerControllerB player, GrabbableObject item)
        {
            item.EnablePhysics(false);
            item.isHeld = true;
            item.playerHeldBy = player;
            item.isPocketed = false;
            item.fallTime = 0f;
            item.hasHitGround = false;

            Transform parentNode = player.IsOwner ? player.localItemHolder : player.serverItemHolder;
            item.parentObject = parentNode;
            item.transform.SetParent(parentNode, false);
            item.transform.localPosition = item.itemProperties.positionOffset;
            item.transform.localEulerAngles = item.itemProperties.rotationOffset;

            // 强行占用当前物品槽
            player.ItemSlots[player.currentItemSlot] = item;
            player.currentlyHeldObjectServer = item;
            player.isHoldingObject = true;

            player.twoHanded = item.itemProperties.twoHanded;
            player.twoHandedAnimation = item.itemProperties.twoHandedAnimation;
            player.playerBodyAnimator.SetBool("Grab", true);
            player.playerBodyAnimator.SetBool("cancelHolding", false);

            if (player.IsOwner)
            {
                HUDManager.Instance.itemSlotIcons[player.currentItemSlot].sprite = item.itemProperties.itemIcon;
                HUDManager.Instance.itemSlotIcons[player.currentItemSlot].enabled = true;
                item.EnableItemMeshes(true); // 确保本地网格可见
            }

            item.EquipItem();
        }

        public override string GetStatus(ulong clientId)
        {
            if (PlayerToShovel.ContainsKey(clientId)) return "<color=#FF00FF>魔术铲已具现</color>";
            string keyName = AwakeningInputs.Instance.UseSkillKey.GetBindingDisplayString();
            return $"<color=#00FF00>就绪 (按 {keyName} 召唤/收回)</color>";
        }

        public static void ResetAll()
        {
            PlayerToShovel.Clear();
            CooldownTimer.Clear();
        }
    }
}