﻿using System;
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
        UseHamstring = true;
        UseBloodRage = false;

        ConfigWinForm(
            new System.Drawing.Point(400, 400), "Z.E.Warrior "
            + Translate.Get("Settings")
        );
    }

    [Category("Combat Rotation")]
    [DefaultValue(true)]
    [DisplayName("Use Hamstring against humanoids")]
    [Description("Use Hamstring against humanoids to prevent them from fleeing too far")]
    public bool UseHamstring { get; set; }

    [Category("Combat Rotation")]
    [DefaultValue(false)]
    [DisplayName("Use Bloodrage")]
    [Description("Use Bloodrage")]
    public bool UseBloodRage { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("ZEWarrior",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("ZEWarrior > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("ZEWarrior",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting = Load<ZEWarriorSettings>(
                    AdviserFilePathAndName("ZEWarrior",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new ZEWarriorSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("ZEWarrior > Load(): " + e);
        }
        return false;
    }
}
