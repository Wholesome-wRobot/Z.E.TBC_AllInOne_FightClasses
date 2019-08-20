﻿using System;
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
        AlwaysPull = false;
        UseEnrage = true;
        UseSwipe = true;
        UseTigersFury = true;
        StealthEngage = true;
        UseBarkskin = true;
        UseTravelForm = false;
        UseInnervate = true;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "WholesomeTBCDruid "
            + Translate.Get("Settings")
        );
    }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Always range pull")]
    [Description("Always pull with a range spell")]
    public bool AlwaysPull { get; set; }

    [Category("Misc")]
    [DefaultValue(false)]
    [DisplayName("Use Travel Form")]
    [Description("Use Travel Form (Triggers more shapeshifts)")]
    public bool UseTravelForm { get; set; }

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