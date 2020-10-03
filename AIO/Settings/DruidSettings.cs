using System;
using System.ComponentModel;
using MarsSettingsGUI;

namespace WholesomeTBCAIO.Settings
{
    [Serializable]
    public class DruidSettings : BasePersistentSettings<DruidSettings>
    {
        public DruidSettings()
        {
            AlwaysPull = false;
            UseBarkskin = true;
            UseTravelForm = true;
            UseInnervate = true;
            CatFormOOC = true;
            UseAquaticForm = true;

            UseEnrage = true;
            UseSwipe = true;
            UseTigersFury = true;
            StealthEngage = true;
            BearFormOnMultiAggro = true;
            NumberOfAttackersBearForm = 2;

            Specialization = "Feral";
        }

        // COMMON
        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Use Travel Form")]
        [Description("Use Travel Form (Triggers more shapeshifts)")]
        public bool UseTravelForm { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Use Aquatic Form")]
        [Description("Use Aquatic Form")]
        public bool UseAquaticForm { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Force Cat Form")]
        [Description("Forces Cat Form when out of combat. Can cause problems with mining and Flights")]
        public bool CatFormOOC { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Use Innervate")]
        [Description("Use Innervate")]
        public bool UseInnervate { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Use Barkskin")]
        [Description("Use Barkskin before healing in dangerous situations")]
        public bool UseBarkskin { get; set; }

        // FERAL
        [Category("Feral")]
        [DefaultValue(false)]
        [DisplayName("Always range pull")]
        [Description("Always pull with a range spell")]
        public bool AlwaysPull { get; set; }

        [Category("Feral")]
        [DefaultValue(true)]
        [DisplayName("Bear form on multi aggro")]
        [Description("Enable/Disable Bear form on multi aggro")]
        public bool BearFormOnMultiAggro { get; set; }

        [Category("Feral")]
        [DefaultValue(2)]
        [DisplayName("Bear number of attackers")]
        [Description("Bear Form when the number of enemies attacking you is superior or equal to this value.")]
        public int NumberOfAttackersBearForm { get; set; }

        [Category("Feral")]
        [DefaultValue(true)]
        [DisplayName("Always use Bear Form Enrage")]
        [Description("Always use Enrage")]
        public bool UseEnrage { get; set; }

        [Category("Feral")]
        [DefaultValue(true)]
        [DisplayName("Swipe")]
        [Description("Use Swipe on multi aggro")]
        public bool UseSwipe { get; set; }

        [Category("Feral")]
        [DefaultValue(true)]
        [DisplayName("Use Tiger's Fury")]
        [Description("Use Tiger's Fury")]
        public bool UseTigersFury { get; set; }

        [Category("Feral")]
        [DefaultValue(true)]
        [DisplayName("Cat Stealth engage")]
        [Description("Try to engage fights using Prowl and going behind the target")]
        public bool StealthEngage { get; set; }

        // TALENT
        [DropdownList(new string[] { "Feral" })]
        public override string Specialization { get; set; }
    }
}