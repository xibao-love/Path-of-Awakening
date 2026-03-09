using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;

namespace Path_of_Awakening.Skills
{
    public class DeadResentmentSkill : AwakeningSkill
    {
        public override string Id => "skill_deadresentment";
        public override string Name => "【被动】亡者遗怨";
        public override string Description => "死亡后产生10m爆炸。5m内队友100点伤害/怪物8点；5-10m内队友60点伤害/怪物4点。";

        public Dictionary<ulong, bool> HasTriggered = new Dictionary<ulong, bool>();

        public override void OnApply(PlayerControllerB player)
        {
            HasTriggered[player.playerClientId] = false;
        }

        public override void OnRemove(PlayerControllerB player)
        {
            HasTriggered.Remove(player.playerClientId);
        }

        public static void HandleExplosion(Vector3 pos)
        {
            if (StartOfRound.Instance == null) return;

            // ================= 1. 暴力特效与音效生成（彻底抛弃原生判定） =================
            if (StartOfRound.Instance.explosionPrefab != null)
            {
                // 在指定坐标直接生成特效预制体，不依赖任何父物体（防止随尸体隐藏被连带销毁）
                GameObject explosion = UnityEngine.Object.Instantiate(StartOfRound.Instance.explosionPrefab, pos, Quaternion.Euler(-90f, 0f, 0f));
                explosion.SetActive(true);

                // 强制播放核心音效（如果预制体自带 AudioSource）
                AudioSource audio = explosion.GetComponent<AudioSource>();
                if (audio != null) audio.Play();

                // 遍历并强制播放所有粒子特效（火光、碎石、烟雾）
                ParticleSystem[] particles = explosion.GetComponentsInChildren<ParticleSystem>();
                foreach (var p in particles)
                {
                    p.Play(true);
                }

                // 5秒后清理特效垃圾，防止内存泄漏
                UnityEngine.Object.Destroy(explosion, 5f);
            }

            // ================= 2. 玩家伤害与【强烈屏幕震动】 =================
            PlayerControllerB localP = GameNetworkManager.Instance.localPlayerController;
            if (localP != null && !localP.isPlayerDead && localP.isPlayerControlled)
            {
                float dist = Vector3.Distance(pos, localP.transform.position);

                if (dist <= 15f && HUDManager.Instance != null)
                {
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                }

                if (dist <= 5f)
                {
                    localP.DamagePlayer(100, true, true, CauseOfDeath.Blast, 0, false, Vector3.zero);
                }
                else if (dist <= 10f)
                {
                    localP.DamagePlayer(60, true, true, CauseOfDeath.Blast, 0, false, Vector3.zero);
                }
            }

            // ================= 3. 怪物伤害 (仅主机负责计算) =================
            if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                EnemyAI[] enemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
                foreach (var enemy in enemies)
                {
                    if (enemy != null && !enemy.isEnemyDead)
                    {
                        float dist = Vector3.Distance(pos, enemy.transform.position);
                        if (dist <= 5f)
                        {
                            enemy.HitEnemy(8, null, true);
                        }
                        else if (dist <= 10f)
                        {
                            enemy.HitEnemy(4, null, true);
                        }
                    }
                }
            }
        }

        public override string GetStatus(ulong clientId)
        {
            if (HasTriggered.TryGetValue(clientId, out bool triggered) && triggered)
            {
                return "<color=#FF0000>已触发 (尸骨无存)</color>";
            }
            return "<color=#00FF00>就绪 (引信已上膛)</color>";
        }
    }
}