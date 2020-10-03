using System;
using System.ComponentModel;
using MarsSettingsGUI;

namespace WholesomeTBCAIO.Settings
{
    [Serializable]
    public class RogueSettings : BasePersistentSettings<RogueSettings>
    {
        public RogueSettings()
        {
            AlwaysPull = false;
            StealthApproach = true;
            StealthWhenPoisoned = false;
            SprintWhenAvail = false;
            UseBlindBandage = true;
            UseGarrote = true;
            ActivateCombatDebug = false;

            RiposteAll = false;

            Specialization = "Combat";
        }

        // COMMON
        [Category("Common")]
        [DefaultValue(false)]
        [DisplayName("Always range pull")]
        [Description("Always pull with a range weapon")]
        public bool AlwaysPull { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Stealth approach")]
        [Description("Always try to approach enemies in Stealth (can be buggy)")]
        public bool StealthApproach { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Use Garrote")]
        [Description("Use Garrote when opening behind the target")]
        public bool UseGarrote { get; set; }

        [Category("Common")]
        [DefaultValue(false)]
        [DisplayName("Stealth poisoned")]
        [Description("Try going in stealth even if affected by poison")]
        public bool StealthWhenPoisoned { get; set; }

        [Category("Common")]
        [DefaultValue(false)]
        [DisplayName("Sprint")]
        [Description("Use Sprint when available")]
        public bool SprintWhenAvail { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Use Blind + Bandage")]
        [Description("Use Blind + the best bandage in your bags during combat " +
            "(If true, you should avoid using poisons and bleed effects, as they will break Blind)")]
        public bool UseBlindBandage { get; set; }

        // COMBAT
        [Category("Combat")]
        [DefaultValue(false)]
        [DisplayName("Riposte all enemies")]
        [Description("On some servers, only humanoids can be riposted. Set this value False if it is the case.")]
        public bool RiposteAll { get; set; }

        // TALENT
        [DropdownList(new string[] { "Combat" })]
        public override string Specialization { get; set; }
    }
}