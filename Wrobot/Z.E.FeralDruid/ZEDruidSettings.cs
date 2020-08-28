using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZEDruidSettings : Settings
{
    public static ZEDruidSettings CurrentSetting { get; set; }

    private ZEDruidSettings()
    {
        ThreadSleepCycle = 10;
        UseDefaultTalents = true;
        AssignTalents = false;
        TalentCodes = new string[] { };
        AlwaysPull = false;
        UseEnrage = true;
        UseSwipe = true;
        UseTigersFury = true;
        StealthEngage = true;
        UseBarkskin = true;
        UseTravelForm = true;
        UseInnervate = true;
        CatFormOOC = true;
        ActivateCombatDebug = false;
        BearFormOnMultiAggro = true;
        NumberOfAttackersBearForm = 2;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCDruid "
            + Translate.Get("Settings")
        );
    }

    [Category("Performance")]
    [DefaultValue(10)]
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

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Always range pull")]
    [Description("Always pull with a range spell")]
    public bool AlwaysPull { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Use Travel Form")]
    [Description("Use Travel Form (Triggers more shapeshifts)")]
    public bool UseTravelForm { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Force Cat Form")]
    [Description("Forces Cat Form when out of combat. Can cause problems with mining and Flights")]
    public bool CatFormOOC { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Use Innervate")]
    [Description("Use Innervate")]
    public bool UseInnervate { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Use Barkskin")]
    [Description("Use Barkskin before healing in dangerous situations")]
    public bool UseBarkskin { get; set; }

    [Category("Bear Form")]
    [DefaultValue(true)]
    [DisplayName("Bear form on multi aggro")]
    [Description("Enable/Disable Bear form on multi aggro")]
    public bool BearFormOnMultiAggro { get; set; }

    [Category("Bear Form")]
    [DefaultValue(2)]
    [DisplayName("Number of attackers")]
    [Description("Bear Form when the number of enemies attacking you is superior or equal to this value.")]
    public int NumberOfAttackersBearForm { get; set; }

    [Category("Bear Form")]
    [DefaultValue(true)]
    [DisplayName("Always use Enrage")]
    [Description("Always use Enrage")]
    public bool UseEnrage { get; set; }

    [Category("Bear Form")]
    [DefaultValue(true)]
    [DisplayName("Use Swipe")]
    [Description("Use Swipe on multi aggro")]
    public bool UseSwipe { get; set; }

    [Category("Cat Form")]
    [DefaultValue(true)]
    [DisplayName("Use Tiger's Fury")]
    [Description("Use Tiger's Fury")]
    public bool UseTigersFury { get; set; }

    [Category("Cat Form")]
    [DefaultValue(true)]
    [DisplayName("Stealth engage")]
    [Description("Try to engage fights using Prowl and going behind the target (can be buggy)")]
    public bool StealthEngage { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Combat log debug")]
    [Description("Activate combat log debug")]
    public bool ActivateCombatDebug { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCDruid",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCDruid > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCDruid",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEDruidSettings>(
                    AdviserFilePathAndName("WholesomeTBCDruid",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEDruidSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCDruid > Load(): " + e);
        }
        return false;
    }
}
