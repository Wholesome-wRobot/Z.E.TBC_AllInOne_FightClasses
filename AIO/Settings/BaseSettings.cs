using System;
using System.Collections.Generic;
using MarsSettingsGUI;
using System.ComponentModel;
using System.Configuration;
using wManager.Wow.ObjectManager;

namespace WholesomeTBCAIO.Settings
{
    [Serializable]
    public abstract class BaseSettings : robotManager.Helpful.Settings
    {
        [Setting]
        [DefaultValue(50)]
        [Category("General")]
        [DisplayName("Refresh rate")]
        [Description("Set this value higher if you have low CPU performance. In doubt, do not change this value.")]
        public int ThreadSleepCycle { get; set; }

        [Setting]
        [Category("Talents")]
        [DisplayName("Talents Codes")]
        [Description("Use a talent calculator to generate your own codes. Do not modify if you are not sure.")]
        public List<string> TalentCodes { get; set; }

        [Setting]
        [Category("Talents")]
        [DefaultValue(true)]
        [DisplayName("Use default talents")]
        [Description("If True, Make sure your talents match the default talents, or reset your talents.")]
        public bool UseDefaultTalents { get; set; }

        [Setting]
        [Category("Talents")]
        [DefaultValue(true)]
        [DisplayName("Auto assign talents")]
        [Description("Will automatically assign your talent points.")]
        public bool AssignTalents { get; set; }
        
        [Setting]
        [Category("Talents")]
        [DefaultValue("Auto")]
        [DisplayName("Specialization")]
        [Description("Choose your specialization")]
        public abstract string Specialization { get; set; }

        [Category("General")]
        [DefaultValue(false)]
        [DisplayName("Combat log debug")]
        [Description("Activate combat log debug")]
        public bool ActivateCombatDebug { get; set; }

        protected BaseSettings()
        {
            ActivateCombatDebug = false;
            ThreadSleepCycle = 100;
            AssignTalents = true;
            TalentCodes = new List<string> { };
            UseDefaultTalents = true;
            Specialization = "Auto";
        }

        protected virtual void OnUpdate() { }

        public void ShowConfiguration()
        {
            var settingWindow = new SettingsWindow(this, ObjectManager.Me.WowClass.ToString());
            settingWindow.ShowDialog();
            OnUpdate();
        }
    }
}
