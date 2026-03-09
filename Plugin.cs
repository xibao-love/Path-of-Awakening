using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Path_of_Awakening
{
    [BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "com.xibao-love.pathofawakening";
        public const string ModName = "Path of Awakening";
        public const string ModVersion = "1.0.0";

        public static AwakeningInputs Inputs { get; internal set; }
        public static ManualLogSource Log = null!; 
        private readonly Harmony harmony = new Harmony(ModGUID);

        private void Awake()
        {
            Log = Logger;

            var loadInputs = AwakeningInputs.Instance;

            this.gameObject.AddComponent<SkillUI>();

            // 扫描当前模组的整个程序集，自动注册里面所有的 [HarmonyPatch] 类
            harmony.PatchAll(typeof(Plugin).Assembly);

            // 初始化技能管理器
            SkillManager.Initialize();

            Log.LogInfo($"[{ModName}] Awakened and loaded successfully!");
        }
    }
}