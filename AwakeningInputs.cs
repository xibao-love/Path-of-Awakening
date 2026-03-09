using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace Path_of_Awakening
{
    public class AwakeningInputs : LcInputActions
    {
        public static readonly AwakeningInputs Instance = new AwakeningInputs();

        [InputAction("<Keyboard>/3", Name = "触发觉醒技能")]
        public InputAction UseSkillKey { get; set; }

        [InputAction("<Keyboard>/f11", Name = "开关觉醒UI面板")]
        public InputAction ToggleUIPanelKey { get; set; }

        [InputAction("<Keyboard>/y", Name = "接受共生契约")]
        public InputAction AcceptContractKey { get; set; }

        [InputAction("<Keyboard>/n", Name = "拒绝共生契约")]
        public InputAction RejectContractKey { get; set; }
    }
}