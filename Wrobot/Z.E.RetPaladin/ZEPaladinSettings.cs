using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

[Serializable]
public class ZEPaladinSettings : Settings
{
    public static ZEPaladinSettings CurrentSetting { get; set; }

    private ZEPaladinSettings()
    {
        ThreadSleepCycle = 100;
        UseDefaultTalents = true;
        AssignTalents = false;
        TalentCodes = new string[] { };
        ManaSaveLimitPercent = 50;
        FlashHealBetweenFights = true;
        UseBlessingOfWisdom = false;
        UseSealOfCommand = false;
        UseExorcism = false;
        UseHammerOfWrath = false;
        DevoAuraOnMulti = true;
        UseSealOfTheCrusader = true;
        HealDuringCombat = true;
        ActivateCombatDebug = false;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCPaladin "
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

    [Category("Combat")]
    [DefaultValue(50)]
    [DisplayName("Mana Save Limit percent")]
    [Description("Try to save this percentage of mana")]
    public int ManaSaveLimitPercent { get; set; }

    [Category("Combat")]
    [DefaultValue(false)]
    [DisplayName("Use Hammer of Wrath")]
    [Description("Use Hammer of Wrath")]
    public bool UseHammerOfWrath { get; set; }

    [Category("Combat")]
    [DefaultValue(false)]
    [DisplayName("Use Exorcism")]
    [Description("Use Exorcism against Undead and Demon target")]
    public bool UseExorcism { get; set; }

    [Category("Misc")]
    [DefaultValue(true)]
    [DisplayName("Flash Heal between fights")]
    [Description("Remain healed up between fights using Flash of Light")]
    public bool FlashHealBetweenFights { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Use Blessing od Wisdom")]
    [Description("Use Blessing od Wisdom instead of Blessing od Might")]
    public bool UseBlessingOfWisdom { get; set; }

    [Category("Combat")]
    [DefaultValue(false)]
    [DisplayName("Use Seal of Command")]
    [Description("Use Seal of Command instead of Seal of Righteousness")]
    public bool UseSealOfCommand { get; set; }

    [Category("Combat")]
    [DefaultValue(true)]
    [DisplayName("Use Seal of the Crusader")]
    [Description("Use Seal of the Crusader when opening a fight")]
    public bool UseSealOfTheCrusader { get; set; }

    [Category("Combat")]
    [DefaultValue(true)]
    [DisplayName("Devotion Aura on multi aggro")]
    [Description("Use Devotion Aura on multi aggro")]
    public bool DevoAuraOnMulti { get; set; }

    [Category("Combat")]
    [DefaultValue(true)]
    [DisplayName("Heal during combat")]
    [Description("Use healing spells during combat")]
    public bool HealDuringCombat { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Combat log debug")]
    [Description("Activate combat log debug")]
    public bool ActivateCombatDebug { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WholesomeTBCPaladin",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCPaladin > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WholesomeTBCPaladin",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEPaladinSettings>(
                    AdviserFilePathAndName("WholesomeTBCPaladin",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEPaladinSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("WholesomeTBCPaladin > Load(): " + e);
        }
        return false;
    }
}
