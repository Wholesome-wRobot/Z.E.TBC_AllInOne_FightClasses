using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZEShamanSettings : Settings
{
    public static ZEShamanSettings CurrentSetting { get; set; }

    private ZEShamanSettings()
    {
        ThreadSleepCycle = 100;
        UseDefaultTalents = true;
        AssignTalents = false;
        TalentCodes = new string[] { };
        UseStoneSkinTotem = false;
        UseLightningShield = false;
        PullRankOneLightningBolt = true;
        PullWithLightningBolt = true;
        UseAirTotems = true;
        UseEarthTotems = true;
        UseFireTotems = true;
        UseWaterTotems = true;
        InterruptWithRankOne = false;
        UseGhostWolf = true;
        UseTotemicCall = true;
        UseMagmaTotem = false;
        UseFlameShock = true;
        ShamanisticRageOnMultiOnly = true;
        UseWaterShield = true;
        ActivateCombatDebug = false;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCShaman "
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
    [DefaultValue(true)]
    [DisplayName("Use Ghost Wolf")]
    [Description("Use Ghost Wolf")]
    public bool UseGhostWolf { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Use Totemic Call")]
    [Description("Use Totemic Call")]
    public bool UseTotemicCall { get; set; }

    [Category("Totems")]
    [DefaultValue(false)]
    [DisplayName("Use Magma Totem")]
    [Description("Use Magma Totem on multi aggro")]
    public bool UseMagmaTotem { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Flame Shock")]
    [Description("Use Flame Shock")]
    public bool UseFlameShock { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(false)]
    [DisplayName("Use Stoneskin Totem")]
    [Description("Use Stoneskin Totem instead of Strength of Earth Totem")]
    public bool UseStoneSkinTotem { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(false)]
    [DisplayName("Use Lightning Shield")]
    [Description("Use Lightning Shield")]
    public bool UseLightningShield { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Water Shield")]
    [Description("Prioritize Water Shield over Lightning Shield")]
    public bool UseWaterShield { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Pull with Lightning Bolt")]
    [Description("Use Lightning Bolt to pull enemies")]
    public bool PullWithLightningBolt { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Pull with rank 1 Lightning Bolt")]
    [Description("Use rank 1 Lightning Bolt to pull enemies (saves mana)")]
    public bool PullRankOneLightningBolt { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(false)]
    [DisplayName("Interrupt with rank 1 Earth Shock")]
    [Description("Use rank 1 Earth Shock to interrupt enemy casting")]
    public bool InterruptWithRankOne { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Only use Shamanistic Rage on multi aggro")]
    [Description("If set to true, will save Shamanistic Rage for multi aggro. If false, will use when available.")]
    public bool ShamanisticRageOnMultiOnly { get; set; }

    [Category("Totems")]
    [DefaultValue(true)]
    [DisplayName("Use Fire totems")]
    [Description("Use Fire totems")]
    public bool UseFireTotems { get; set; }

    [Category("Totems")]
    [DefaultValue(true)]
    [DisplayName("Use Air totems")]
    [Description("Use Air totems")]
    public bool UseAirTotems { get; set; }

    [Category("Totems")]
    [DefaultValue(true)]
    [DisplayName("Use Water totems")]
    [Description("Use Water totems")]
    public bool UseWaterTotems { get; set; }

    [Category("Totems")]
    [DefaultValue(true)]
    [DisplayName("Use Earth totems")]
    [Description("Use Earth totems")]
    public bool UseEarthTotems { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Combat log debug")]
    [Description("Activate combat log debug")]
    public bool ActivateCombatDebug { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCShaman",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCShaman > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCShaman",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEShamanSettings>(
                    AdviserFilePathAndName("WholesomeTBCShaman",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEShamanSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCShaman > Load(): " + e);
        }
        return false;
    }
}
