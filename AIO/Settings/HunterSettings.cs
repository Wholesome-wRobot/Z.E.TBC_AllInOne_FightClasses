using System;
using System.ComponentModel;
using MarsSettingsGUI;

namespace WholesomeTBCAIO.Settings
{
    [Serializable]
    public class HunterSettings : BasePersistentSettings<HunterSettings>
    {
        public HunterSettings()
        {
            RangedWeaponSpeed = 2500;
            BackupFromMelee = true;
            MaxBackupAttempts = 3;
            BackupUsingCTM = false;
            UseFreezingTrap = true;
            FeedPet = true;
            RapidFireOnMulti = false;
            AutoGrowl = false;
            UseAspectOfTheCheetah = true;

            BestialWrathOnMulti = false;

            Specialization = "BeastMaster";
        }

        // COMMON
        [Category("Common")]
        [DefaultValue(2500)]
        [DisplayName("Weapon speed")]
        [Description("Ranged weapon speed in milliseconds (used to delay Steady shot)")]
        public int RangedWeaponSpeed { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Backup from melee")]
        [Description("Set to True is you want to backup from melee range when your pet has gained aggro")]
        public bool BackupFromMelee { get; set; }

        [Category("Common")]
        [DefaultValue(false)]
        [DisplayName("Backup using CTM")]
        [Description("If set to True, will backup using Click To Move. If false, will use the keyboard")]
        public bool BackupUsingCTM { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Use freezing trap")]
        [Description("Set to True is you want to use freezing trap on multiple aggro (will trigger if Mend Pet is active on primary target)")]
        public bool UseFreezingTrap { get; set; }

        [Category("Common")]
        [DefaultValue(5)]
        [DisplayName("Max backup attempts")]
        [Description("Maximum number of attempts after failing to backup to a valid distance (eg when back to a wall)")]
        public int MaxBackupAttempts { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Feed Pet")]
        [Description("Use Z.E.Hunter to manage pet feeding")]
        public bool FeedPet { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Aspect of the Cheetah")]
        [Description("Use Aspect of the Cheetah")]
        public bool UseAspectOfTheCheetah { get; set; }

        [Category("Common")]
        [DefaultValue(false)]
        [DisplayName("Rapid Fire on multi")]
        [Description("Only use Rapid Fire on multi aggro. If set to False, Rapid Fire will be used as soon at available")]
        public bool RapidFireOnMulti { get; set; }

        [Category("Common")]
        [DefaultValue(false)]
        [DisplayName("Auto Growl")]
        [Description("If true, will let Growl on autocast. If false, will let the AIO manage Growl in order to save your pet's energy.")]
        public bool AutoGrowl { get; set; }

        // BEAST MASTERY
        [Category("BeastMastery")]
        [DefaultValue(false)]
        [DisplayName("BW on multi")]
        [Description("Only use Bestial Wrath on multi aggro. If set to False, Bestial Wrath will be used as soon at available")]
        public bool BestialWrathOnMulti { get; set; }

        // TALENT
        [DropdownList(new string[] { "BeastMaster" })]
        public override string Specialization { get; set; }
    }
}