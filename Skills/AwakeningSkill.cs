using GameNetcodeStuff;

namespace Path_of_Awakening.Skills
{
    public abstract class AwakeningSkill
    {
        // 技能的唯一ID、名称和描述
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }

        // 当玩家获得该技能时触发 (赋予属性、添加组件等)
        public virtual void OnApply(PlayerControllerB player) { }

        // 当一天结束，技能被清除时触发 (还原属性、移除组件等)
        public virtual void OnRemove(PlayerControllerB player) { }

        // 可选：如果技能需要每帧执行逻辑（如持续回血），可以重写这个方法
        public virtual void OnUpdate(PlayerControllerB player) { }

        // 获取该玩家当前技能状态的文字描述，默认返回“被动生效中”
        public virtual string GetStatus(ulong clientId)
        {
            return "觉醒生效中";
        }

        // 当主动技能被按键触发时执行
        public virtual void OnActivate(PlayerControllerB player) { }
    }
}