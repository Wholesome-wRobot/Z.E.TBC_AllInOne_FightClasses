using System;
using System.ComponentModel;
using MarsSettingsGUI;

namespace WholesomeTBCAIO.Settings
{
    [Serializable]
    public class MageSettings : BasePersistentSettings<MageSettings>
    {
        public MageSettings()
        {
            BackupUsingCTM = true;
            UsePolymorph = true;
            WandThreshold = 30;
            BlinkWhenBackup = true;

            UseConeOfCold = true;
            IcyVeinMultiPull = true;
            FireblastThreshold = 30;

            Specialization = "Frost";
        }

        // COMMON
        [Category("Common")]
        [DefaultValue(30)]
        [DisplayName("Wand Threshold")]
        [Description("Enemy HP % under which the wand should be used")]
        [Percentage(true)]
        public int WandThreshold { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Use Polymorph")]
        [Description("Use Polymorph on multiaggro")]
        public bool UsePolymorph { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Backup using CTM")]
        [Description("If set to True, will backup using Click To Move. If false, will use the keyboard")]
        public bool BackupUsingCTM { get; set; }

        // FROST
        [Category("Frost")]
        [DefaultValue(true)]
        [DisplayName("Use Cone of Cold")]
        [Description("Use Cone of Cold during the combat rotation")]
        public bool UseConeOfCold { get; set; }

        [Category("Frost")]
        [DefaultValue(30)]
        [DisplayName("Fire Blast Threshold")]
        [Description("Enemy HP % under which Fire Blast should be used")]
        [Percentage(true)]
        public int FireblastThreshold { get; set; }

        [Category("Frost")]
        [DefaultValue(true)]
        [DisplayName("Icy Veins on multi")]
        [Description("Only use Icy Veins when 2 or more enemy are pulled")]
        public bool IcyVeinMultiPull { get; set; }

        [Category("Frost")]
        [DefaultValue(true)]
        [DisplayName("Blink when backup")]
        [Description("Use Blink when backing up from the target")]
        public bool BlinkWhenBackup { get; set; }

        // TALENT
        [DropdownList(new string[] { "Enhancement", "Elemental" })]
        public override string Specialization { get; set; }
    }
}