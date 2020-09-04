using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZEBMHunterSettings : Settings
{
    public static ZEBMHunterSettings CurrentSetting { get; set; }

    private ZEBMHunterSettings()
    {
        ThreadSleepCycle = 100;
        UseDefaultTalents = true;
        AssignTalents = false;
        TalentCodes = new string[] { };
        RangedWeaponSpeed = 2500;
        BackupFromMelee = true;
        UseFreezingTrap = true;
        MaxBackupAttempts = 3;
        FeedPet = true;
        BestialWrathOnMulti = false;
        RapidFireOnMulti = false;
        AutoGrowl = false;
        ActivateCombatDebug = false;
        BackupUsingCTM = false;
        UseAspectOfTheCheetah = true;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCHunter "
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
    [DefaultValue(2500)]
    [DisplayName("Ranged weapon speed")]
    [Description("Ranged weapon speed in milliseconds")]
    public int RangedWeaponSpeed { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Backup from melee")]
    [Description("Set to True is you want to backup from melee range when your pet has gained aggro")]
    public bool BackupFromMelee { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(false)]
    [DisplayName("Backup using CTM")]
    [Description("If set to True, will backup using Click To Move. If false, will use the keyboard")]
    public bool BackupUsingCTM { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use freezing trap")]
    [Description("Set to True is you want to use freezing trap on multiple aggro (will trigger if Mend Pet is active on primary target)")]
    public bool UseFreezingTrap { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(5)]
    [DisplayName("Max backup attempts")]
    [Description("Maximum number of attempts after failing to backup to a valid distance (eg when back to a wall)")]
    public int MaxBackupAttempts { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Feed Pet")]
    [Description("Use Z.E.Hunter to manage pet feeding")]
    public bool FeedPet { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Use Aspect of the Cheetah")]
    [Description("Use Aspect of the Cheetah")]
    public bool UseAspectOfTheCheetah { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Bestial Wrath on multi aggro")]
    [Description("Only use Bestial Wrath on multi aggro. If set to False, Bestial Wrath will be used as soon at available")]
    public bool BestialWrathOnMulti { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Rapid Fire on multi aggro")]
    [Description("Only use Rapid Fire on multi aggro. If set to False, Rapid Fire will be used as soon at available")]
    public bool RapidFireOnMulti { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(false)]
    [DisplayName("Auto Growl")]
    [Description("If true, will let Growl on autocast. If false, will let Z.E.Hunter manage Growl in order to save your pet's energy.")]
    public bool AutoGrowl { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Combat log debug")]
    [Description("Activate combat log debug")]
    public bool ActivateCombatDebug { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCHunter",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Main.LogError("WholesomeTBCHunter > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCHunter",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEBMHunterSettings>(
                    AdviserFilePathAndName("WholesomeTBCHunter",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEBMHunterSettings();
        }
        catch (Exception e)
        {
            Main.LogError("WholesomeTBCHunter > Load(): " + e);
        }
        return false;
    }
}
