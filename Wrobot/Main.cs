﻿using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Drawing;
using wManager.Events;
using System.ComponentModel;

public class Main : ICustomClass
{
    public static readonly float DefaultMeleeRange = 5f;

    private static float _settingRange = DefaultMeleeRange;
    private static bool _debug = false;
    private static bool _saveCalcuCombatRangeSetting = wManager.wManagerSetting.CurrentSetting.CalcuCombatRange;
    private static readonly BackgroundWorker _talentThread = new BackgroundWorker();

    public static string wowClass = ObjectManager.Me.WowClass.ToString();
    public static int _humanReflexTime = 500;
    public static bool _isLaunched;
    public static string version = "1.5.7"; // Must match version in Version.txt
    public bool haveCheckedForUpdate = false;
    public static bool HMPrunningAway = false;
    public static string wowVersion;

    public float Range 
	{
		get { return _settingRange; }
    }

    public void Initialize()
    {
        ZETBCFCSettings.Load();
        AutoUpdater.CheckUpdate(version);

        Log($"FC version {version}. Discovering class and finding rotation...");
        var type = Type.GetType(wowClass);

        wowVersion = Lua.LuaDoString<string>("v, b, d, t = GetBuildInfo(); return v");
        Log($"Wow version : {wowVersion}");

        if (type != null)
        {
            _isLaunched = true;
            
            // Fight end
            FightEvents.OnFightEnd += (ulong guid) =>
            {
                wManager.wManagerSetting.CurrentSetting.CalcuCombatRange = _saveCalcuCombatRangeSetting;
                HMPrunningAway = false;
            };

            // Fight start
            FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
            {
                wManager.wManagerSetting.CurrentSetting.CalcuCombatRange = false;
                HMPrunningAway = false;
            };

            // HMP run away handler
            robotManager.Events.LoggingEvents.OnAddLog += (Logging.Log log) =>
            {
                if (log.Text == "[HumanMasterPlugin] Starting to run away")
                {
                    Log("HMP's running away feature detected. Disabling FightClass");
                    HMPrunningAway = true;
                }
                else if (log.Text == "[HumanMasterPlugin] Stop fleeing, allow attacks again")
                {
                    Log("Reenabling FightClass");
                    HMPrunningAway = false;
                }
            };

            if (!Talents._isRunning)
            {
                _talentThread.DoWork += Talents.DoTalentPulse;
                _talentThread.RunWorkerAsync();
            }

            type.GetMethod("Initialize").Invoke(null, null);
        }
        else
        {
            LogError("Class not supported.");
            Products.ProductStop();
        }
    }

    public void Dispose()
    {
        wManager.wManagerSetting.CurrentSetting.CalcuCombatRange = _saveCalcuCombatRangeSetting;
        var type = Type.GetType(wowClass);
        if (type != null)
            type.GetMethod("Dispose").Invoke(null, null);
        _isLaunched = false;
        _talentThread.DoWork -= Talents.DoTalentPulse;
        _talentThread.Dispose();
        Talents._isRunning = false;
    }

    public void ShowConfiguration()
    {
        var type = Type.GetType(wowClass);

        if (type != null)
            type.GetMethod("ShowConfiguration").Invoke(null, null);
        else
            LogError("Class not supported.");
    }

    public static void LogFight(string message)
    {
        Logging.Write($"[Wholesome-FC-TBC - {wowClass}]: { message}", Logging.LogType.Fight, Color.ForestGreen);
    }

    public static void LogError(string message)
    {
        Logging.Write($"[Wholesome-FC-TBC - {wowClass}]: {message}", Logging.LogType.Error, Color.DarkRed);
    }

    public static void Log(string message)
    {
        Logging.Write($"[Wholesome-FC-TBC - {wowClass}]: {message}", Logging.LogType.Normal, Color.DarkSlateBlue);
    }

    public static void Log(string message, Color c)
    {
        Logging.Write($"[Wholesome-FC-TBC - {wowClass}]: {message}", Logging.LogType.Normal, c);
    }

    public static void LogDebug(string message)
    {
        if (_debug)
            Logging.WriteDebug($"[Wholesome-FC-TBC - {wowClass}]: { message}");
    }

    public static void CombatDebug(string message)
    {
        Logging.Write($"[Wholesome-FC-TBC - {wowClass}]: { message}", Logging.LogType.Normal, Color.Plum);
    }

    public static void SetRange(float range)
    {
        if (range != _settingRange)
        {
            _settingRange = range;
            LogDebug($"Range set to {_settingRange}");
        }
    }

    public static void SetRangeToMelee()
    {
        if (ObjectManager.Target != null)
            SetRange(DefaultMeleeRange + (ObjectManager.Target.CombatReach / 2.5f));
        else
            SetRange(DefaultMeleeRange);
    }

    public static bool CurrentRangeIsMelee()
    {
        if (ObjectManager.Target != null)
            return (decimal)GetRange() == (decimal)(DefaultMeleeRange + (ObjectManager.Target.CombatReach / 2.5f));
        else
            return (decimal)GetRange() == (decimal)DefaultMeleeRange;
    }

    public static float GetRange()
    {
        return _settingRange;
    }
}
