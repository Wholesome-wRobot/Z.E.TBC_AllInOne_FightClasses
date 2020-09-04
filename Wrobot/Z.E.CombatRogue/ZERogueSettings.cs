using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZERogueSettings : Settings
{
    public static ZERogueSettings CurrentSetting { get; set; }

    private ZERogueSettings()
    {
        ThreadSleepCycle = 100;
        UseDefaultTalents = true;
        AssignTalents = false;
        TalentCodes = new string[] { };
        AlwaysPull = false;
        StealthApproach = true;
        StealthWhenPoisoned = false;
        SprintWhenAvail = false;
        UseBlindBandage = true;
        UseGarrote = true;
        RiposteAll = false;
        ActivateCombatDebug = false;

        ConfigWinForm(
            new System.Drawing.Point(500, 400), "WholesomeTBCRogue "
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

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Always range pull")]
    [Description("Always pull with a range weapon")]
    public bool AlwaysPull { get; set; }

    [Category("Fight engage")]
    [DefaultValue(true)]
    [DisplayName("Stealth approach")]
    [Description("Always try to approach enemies in Stealth (can be buggy)")]
    public bool StealthApproach { get; set; }

    [Category("Fight engage")]
    [DefaultValue(true)]
    [DisplayName("Use Garrote")]
    [Description("Use Garrote when opening behind the target")]
    public bool UseGarrote { get; set; }

    [Category("Fight engage")]
    [DefaultValue(false)]
    [DisplayName("Stealth even if poisoned")]
    [Description("Try going in stealth even if affected by poison")]
    public bool StealthWhenPoisoned { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Sprint when available")]
    [Description("Use Sprint when available")]
    public bool SprintWhenAvail { get; set; }

    [Category("Combat")]
    [DefaultValue(true)]
    [DisplayName("Use Blind + Bandage")]
    [Description("Use Blind + the best bandage in your bags during combat " +
        "(If true, you should avoid using poisons and bleed effects, as they will break Blind)")]
    public bool UseBlindBandage { get; set; }

    [Category("Combat")]
    [DefaultValue(false)]
    [DisplayName("Riposte all enemies")]
    [Description("On some servers, only humanoids can be riposted. Set this value False if it is the case.")]
    public bool RiposteAll { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Combat log debug")]
    [Description("Activate combat log debug")]
    public bool ActivateCombatDebug { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCRogue",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCRogue > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCRogue",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZERogueSettings>(
                    AdviserFilePathAndName("WholesomeTBCRogue",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZERogueSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCRogue > Load(): " + e);
        }
        return false;
    }
}
