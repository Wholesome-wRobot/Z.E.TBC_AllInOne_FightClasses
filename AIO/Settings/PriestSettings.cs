using System;
using System.ComponentModel;
using MarsSettingsGUI;

namespace WholesomeTBCAIO.Settings
{
    [Serializable]
    public class PriestSettings : BasePersistentSettings<PriestSettings>
    {
        public PriestSettings()
        {
            WandThreshold = 40;
            UseInnerFire = true;
            UseShieldOnPull = true;
            UseShadowGuard = true;
            UseShadowProtection = true;
            UsePowerWordShield = true;

            UseShadowWordDeath = true;

            Specialization = "Shadow";
        }

        // COMMON
        [Category("Common")]
        [DefaultValue(40)]
        [DisplayName("Wand Threshold")]
        [Description("Enemy HP under which the wand should be used")]
        [Percentage(true)]
        public int WandThreshold { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Power Word: Shield")]
        [Description("Use Power Word: Shield")]
        public bool UsePowerWordShield { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Shield on pull")]
        [Description("Use Power Word: Shield on pull")]
        public bool UseShieldOnPull { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Shadowguard")]
        [Description("Use Shadowguard")]
        public bool UseShadowGuard { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Shadow Protection")]
        [Description("Use Shadow Protection")]
        public bool UseShadowProtection { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Inner Fire")]
        [Description("Use Inner Fire")]
        public bool UseInnerFire { get; set; }

        // SHADOW
        [Category("Shadow")]
        [DefaultValue(true)]
        [DisplayName("SW: Death")]
        [Description("Use Shadow Word: Death")]
        public bool UseShadowWordDeath { get; set; }

        // TALENT
        [DropdownList(new string[] { "Shadow" })]
        public override string Specialization { get; set; }
    }
}