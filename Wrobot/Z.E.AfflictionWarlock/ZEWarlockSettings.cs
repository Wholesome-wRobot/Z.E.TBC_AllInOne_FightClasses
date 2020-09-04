using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZEWarlockSettings : Settings
{
    public static ZEWarlockSettings CurrentSetting { get; set; }

    private ZEWarlockSettings()
    {
        ThreadSleepCycle = 100;
        UseDefaultTalents = true;
        AssignTalents = false;
        TalentCodes = new string[] { };
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

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCWarlock "
            + Translate.Get("Settings")
        );
    }

    [Category("Performance")]
    [DefaultValue(100)]
    [DisplayName("Refresh rate (ms)")]
    [Description("Set this value higher if you have low CPU performance. In doubt, do not change this value.")]
    public int ThreadSleepCycle { get; set; }

    [Category("Talents")]
    [DisplayName("Talents Codes")]
    [Description("Use a talent calculator to generate your own codes: https://talentcalculator.org/tbc/. " +
        "Do not modify if you are not sure.")]
    public string[] TalentCodes { get; set; }

    [Category("Talents")]
    [DefaultValue(true)]
    [DisplayName("Use default talents")]
    [Description("If True, Make sure your talents match the default talents, or reset your talents.")]
    public bool UseDefaultTalents { get; set; }

    [Category("Talents")]
    [DefaultValue(false)]
    [DisplayName("Auto assign talents")]
    [Description("Will automatically assign your talent points.")]
    public bool AssignTalents { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Life Tap")]
    [Description("Use Life Tap")]
    public bool UseLifeTap { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Soul Shatter")]
    [Description("Use Soul Shatter on multi aggro")]
    public bool UseSoulShatter { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Incinerate")]
    [Description("Use Incinerate (Use Immolate at high level must be True)")]
    public bool UseIncinerate { get; set; }
    /*
    [Category("Combat Rotation")]
    [DefaultValue(false)]
    [DisplayName("Fear additional enemies")]
    [Description("Switch target and fear on multi aggro")]
    public bool FearAdds { get; set; }
    */
    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Prioritize wanding over Shadow Bolt")]
    [Description("Prioritize wanding over Shadow Bolt during combat to save mana")]
    public bool PrioritizeWandingOverSB { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(false)]
    [DisplayName("Auto torment")]
    [Description("If true, will let Torment on autocast. If false, will let Z.E.Warlock manage Torment in order to save Voidwalker mana.")]
    public bool AutoTorment { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Immolate at high level")]
    [Description("Keep using Immmolate once Unstable Affliction is learnt")]
    public bool UseImmolateHighLevel { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(false)]
    [DisplayName("Use Siphon Life")]
    [Description("Use Siphon Life (Recommended only after TBC green gear)")]
    public bool UseSiphonLife { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Put pet in passive when out of combat")]
    [Description("Puts pet in passive when out of combat (can be useful if you want to ignore fights when traveling)")]
    public bool PetInPassiveWhenOOC { get; set; }

    [Category("Misc")]
    [DefaultValue(4)]
    [DisplayName("Number of Soul Shards")]
    [Description("Sets the minimum number of Soul Shards to have in your bags")]
    public int NumberOfSoulShards { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Use Unending Breath")]
    [Description("Makes sure you have Unending Breath up at all time")]
    public bool UseUnendingBreath { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Use Dark Pact")]
    [Description("Use Dark Pact")]
    public bool UseDarkPact { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Use Fel Armor")]
    [Description("Use Fel Armor instead of Demon Armor")]
    public bool UseFelArmor { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Use Soul Stone")]
    [Description("Use Soul Stone (needs a third party plugin to resurrect using the Soulstone)")]
    public bool UseSoulStone { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Combat log debug")]
    [Description("Activate combat log debug")]
    public bool ActivateCombatDebug { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCWarlock",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCWarlock > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCWarlock",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEWarlockSettings>(
                    AdviserFilePathAndName("WholesomeTBCWarlock",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEWarlockSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCWarlock > Load(): " + e);
        }
        return false;
    }
}
