using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;
using System.Collections.Generic;
using MarsSettingsGUI;

namespace WholesomeTBCAIO.Settings
{
    [Serializable]
    public class WarriorSettings : BasePersistentSettings<WarriorSettings>
    {
        public WarriorSettings()
        {
            UseHamstring = true;
            UseBloodRage = true;
            UseDemoralizingShout = true;
            UseRend = true;
            UseCleave = true;
            AlwaysPull = false;
            UseCommandingShout = false;

            PrioritizeBerserkStance = false;

            Specialization = "Fury";
        }

        // COMMON
        [Category("Common")]
        [DefaultValue(false)]
        [DisplayName("Always range pull")]
        [Description("Always pull with a range weapon")]
        public bool AlwaysPull { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Hamstring humanoids")]
        [Description("Use Hamstring against humanoids to prevent them from fleeing too far")]
        public bool UseHamstring { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Bloodrage")]
        [Description("Use Bloodrage")]
        public bool UseBloodRage { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Demoralizing Shout")]
        [Description("Use Demoralizing Shout")]
        public bool UseDemoralizingShout { get; set; }

        [Category("Common")]
        [DefaultValue(false)]
        [DisplayName("Commanding Shout")]
        [Description("Use Commanding Shout instead of Battle Shout")]
        public bool UseCommandingShout { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Rend")]
        [Description("Use Rend")]
        public bool UseRend { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Cleave")]
        [Description("Use Cleave on multi aggro")]
        public bool UseCleave { get; set; }

        // FURY
        [Category("Fury")]
        [DefaultValue(false)]
        [DisplayName("Prioritize Berserker")]
        [Description("Prioritize Berserker Stance over Battle Stance")]
        public bool PrioritizeBerserkStance { get; set; }

        // TALENT
        [DropdownList(new string[] { "Fury" })]
        public override string Specialization { get; set; }
    }
}