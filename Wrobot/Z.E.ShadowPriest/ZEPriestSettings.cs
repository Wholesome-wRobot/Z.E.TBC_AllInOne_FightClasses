using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZEPriestSettings : Settings
{
    public static ZEPriestSettings CurrentSetting { get; set; }

    private ZEPriestSettings()
    {
        ThreadSleepCycle = 100;
        WandThreshold = 40;
        UseInnerFire = true;
        UseShieldOnPull = true;
        UseShadowGuard = true;
        UseShadowProtection = true;
        UseShadowWordDeath = true;
        UsePowerWordShield = true;
        UseDefaultTalents = true;
        AssignTalents = false;
        TalentCodes = new string[] { };
        ActivateCombatDebug = false;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCPriest "
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
    [DefaultValue(40)]
    [DisplayName("Wand Threshold")]
    [Description("Enemy HP under which the wand should be used")]
    public int WandThreshold { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Power Word: Shield")]
    [Description("Use Power Word: Shield")]
    public bool UsePowerWordShield { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Shield on pull")]
    [Description("Use Power Word: Shield on pull")]
    public bool UseShieldOnPull { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Shadow Word: Death")]
    [Description("Use Shadow Word: Death")]
    public bool UseShadowWordDeath { get; set; }

    [Category("Buffs")]
    [DefaultValue(true)]
    [DisplayName("Use Shadowguard")]
    [Description("Use Shadowguard")]
    public bool UseShadowGuard { get; set; }

    [Category("Buffs")]
    [DefaultValue(true)]
    [DisplayName("Use Shadow Protection")]
    [Description("Use Shadow Protection")]
    public bool UseShadowProtection { get; set; }

    [Category("Buffs")]
    [DefaultValue(true)]
    [DisplayName("Use Inner Fire")]
    [Description("Use Inner Fire")]
    public bool UseInnerFire { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Combat log debug")]
    [Description("Activate combat log debug")]
    public bool ActivateCombatDebug { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCPriest",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCPriest > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCPriest",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEPriestSettings>(
                    AdviserFilePathAndName("WholesomeTBCPriest",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEPriestSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCPriest > Load(): " + e);
        }
        return false;
    }
}
