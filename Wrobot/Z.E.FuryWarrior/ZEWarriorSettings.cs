using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZEWarriorSettings : Settings
{
    public static ZEWarriorSettings CurrentSetting { get; set; }

    private ZEWarriorSettings()
    {
        ThreadSleepCycle = 100;
        UseDefaultTalents = true;
        AssignTalents = false;
        TalentCodes = new string[] { };
        UseHamstring = true;
        UseBloodRage = true;
        UseDemoralizingShout = true;
        UseRend = true;
        UseCleave = true;
        PrioritizeBerserkStance = false;
        AlwaysPull = false;
        UseCommandingShout = false;
        ActivateCombatDebug = false;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCWarrior "
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
    [DisplayName("Prioritize Berserker Stance")]
    [Description("Prioritize Berserker Stance over Battle Stance")]
    public bool PrioritizeBerserkStance { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Always range pull")]
    [Description("Always pull with a range weapon")]
    public bool AlwaysPull { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Hamstring against humanoids")]
    [Description("Use Hamstring against humanoids to prevent them from fleeing too far")]
    public bool UseHamstring { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Bloodrage")]
    [Description("Use Bloodrage")]
    public bool UseBloodRage { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Demoralizing Shout")]
    [Description("Use Demoralizing Shout")]
    public bool UseDemoralizingShout { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(false)]
    [DisplayName("Use Commanding Shout")]
    [Description("Use Commanding Shout instead of Battle Shout")]
    public bool UseCommandingShout { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Rend")]
    [Description("Use Rend")]
    public bool UseRend { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Cleave")]
    [Description("Use Cleave on multi aggro")]
    public bool UseCleave { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Combat log debug")]
    [Description("Activate combat log debug")]
    public bool ActivateCombatDebug { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCWarrior",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCWarrior > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCWarrior",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEWarriorSettings>(
                    AdviserFilePathAndName("WholesomeTBCWarrior",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEWarriorSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCWarrior > Load(): " + e);
        }
        return false;
    }
}
