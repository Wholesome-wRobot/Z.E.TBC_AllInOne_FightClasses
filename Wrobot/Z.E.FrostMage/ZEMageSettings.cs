using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZEMageSettings : Settings
{
    public static ZEMageSettings CurrentSetting { get; set; }

    private ZEMageSettings()
    {
        ThreadSleepCycle = 10;
        UseDefaultTalents = true;
        AssignTalents = false;
        TalentCodes = new string[] { };
        UseConeOfCold = true;
        WandThreshold = 30;
        IcyVeinMultiPull = true;
        BlinkWhenBackup = true;
        ActivateCombatDebug = false;
        FireblastThreshold = 30;
        BackupUsingCTM = true;
        UsePolymorph = true;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCMage "
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

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Cone of Cold")]
    [Description("Use Cone of Cold during the combat rotation")]
    public bool UseConeOfCold { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(30)]
    [DisplayName("Wand Threshold")]
    [Description("Enemy HP % under which the wand should be used")]
    public int WandThreshold { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(30)]
    [DisplayName("Fire Blast Threshold")]
    [Description("Enemy HP % under which Fire Blast should be used")]
    public int FireblastThreshold { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Only use Icy Veins on multipull")]
    [Description("Only use Icy Veins when 2 or more enemy are pulled")]
    public bool IcyVeinMultiPull { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Blink when backing up")]
    [Description("Use Blink when backing up from the target")]
    public bool BlinkWhenBackup { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Polymorph")]
    [Description("Use Polymorph on multiaggro")]
    public bool UsePolymorph { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Backup using CTM")]
    [Description("If set to True, will backup using Click To Move. If false, will use the keyboard")]
    public bool BackupUsingCTM { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Combat log debug")]
    [Description("Activate combat log debug")]
    public bool ActivateCombatDebug { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCMage",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCMage > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCMage",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEMageSettings>(
                    AdviserFilePathAndName("WholesomeTBCMage",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEMageSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCMage > Load(): " + e);
        }
        return false;
    }
}
