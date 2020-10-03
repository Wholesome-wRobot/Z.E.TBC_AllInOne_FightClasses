using System;
using System.ComponentModel;
using MarsSettingsGUI;

namespace WholesomeTBCAIO.Settings
{
    [Serializable]
    public class WarlockSettings : BasePersistentSettings<WarlockSettings>
    {
        public WarlockSettings()
        {
            UseLifeTap = true;
            PetInPassiveWhenOOC = true;
            PrioritizeWandingOverSB = true;
            UseSiphonLife = false;
            UseImmolateHighLevel = true;
            UseUnendingBreath = true;
            UseDarkPact = true;
            UseSoulStone = true;
            AutoTorment = false;
            UseFelArmor = true;
            UseIncinerate = true;
            UseSoulShatter = true;
            NumberOfSoulShards = 4;
            ActivateCombatDebug = false;
            //FearAdds = false;

            Specialization = "Affliction";
        }

        // COMMON
        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Life Tap")]
        [Description("Use Life Tap")]
        public bool UseLifeTap { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Soul Shatter")]
        [Description("Use Soul Shatter on multi aggro")]
        public bool UseSoulShatter { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Wand over SB")]
        [Description("Prioritize wanding over Shadow Bolt during combat to save mana")]
        public bool PrioritizeWandingOverSB { get; set; }

        [Category("Common")]
        [DefaultValue(false)]
        [DisplayName("Auto torment")]
        [Description("If true, will let Torment on autocast. If false, will let the AIO manage Torment in order to save Voidwalker mana.")]
        public bool AutoTorment { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Passive Pet OOC")]
        [Description("Puts pet in passive when out of combat (can be useful if you want to ignore fights when traveling)")]
        public bool PetInPassiveWhenOOC { get; set; }

        [Category("Common")]
        [DefaultValue(4)]
        [DisplayName("Soul Shards")]
        [Description("Sets the minimum number of Soul Shards to have in your bags")]
        public int NumberOfSoulShards { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Unending Breath")]
        [Description("Makes sure you have Unending Breath up at all time")]
        public bool UseUnendingBreath { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Dark Pact")]
        [Description("Use Dark Pact")]
        public bool UseDarkPact { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Fel Armor")]
        [Description("Use Fel Armor instead of Demon Armor")]
        public bool UseFelArmor { get; set; }

        [Category("Common")]
        [DefaultValue(true)]
        [DisplayName("Soul Stone")]
        [Description("Use Soul Stone (needs a third party plugin to resurrect using the Soulstone)")]
        public bool UseSoulStone { get; set; }

        // AFFLICTION
        [Category("Affliction")]
        [DefaultValue(true)]
        [DisplayName("Incinerate")]
        [Description("Use Incinerate (Use Immolate at high level must be True)")]
        public bool UseIncinerate { get; set; }

        [Category("Affliction")]
        [DefaultValue(true)]
        [DisplayName("Immolate high level")]
        [Description("Keep using Immmolate once Unstable Affliction is learnt")]
        public bool UseImmolateHighLevel { get; set; }

        [Category("Affliction")]
        [DefaultValue(false)]
        [DisplayName("Siphon Life")]
        [Description("Use Siphon Life (Recommended only after TBC green gear)")]
        public bool UseSiphonLife { get; set; }

        /*
        [Category("Combat Rotation")]
        [DefaultValue(false)]
        [DisplayName("Fear additional enemies")]
        [Description("Switch target and fear on multi aggro")]
        public bool FearAdds { get; set; }
        */

        // TALENT
        [DropdownList(new string[] { "Affliction" })]
        public override string Specialization { get; set; }
    }
}