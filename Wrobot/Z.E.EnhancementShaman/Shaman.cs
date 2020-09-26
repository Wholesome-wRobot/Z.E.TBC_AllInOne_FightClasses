using System;
using System.Diagnostics;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using robotManager.FiniteStateMachine;
using System.ComponentModel;
using System.Collections.Generic;

public static class Shaman
{
    internal static Stopwatch _ghostWolfTimer = new Stopwatch();
    internal static Stopwatch _pullMeleeTimer = new Stopwatch();
    internal static Stopwatch _meleeTimer = new Stopwatch();
    internal static Vector3 _fireTotemPosition = null;
    private static WoWLocalPlayer Me = ObjectManager.Me;
    internal static ZEShamanSettings _settings;
    private static bool _goInMelee = false;
    private static bool _fightingACaster = false;
    private static float _pullRange = 28f;
    internal static int _lowManaThreshold = 20;
    internal static int _mediumManaThreshold = 50;
    static List<string> _casterEnemies = new List<string>();
    static TotemManager totemManager = new TotemManager();
    private static int _pullAttempt;

    public static void Initialize()
    {
        Main.Log("Initialized");
        ZEShamanSettings.Load();
        _settings = ZEShamanSettings.CurrentSetting;
        Talents.InitTalents(_settings.AssignTalents, _settings.UseDefaultTalents, _settings.TalentCodes);
        _ghostWolfTimer.Start();

        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _goInMelee = false;
            _ghostWolfTimer.Restart();
            _fightingACaster = false;
            _meleeTimer.Reset();
            _pullMeleeTimer.Reset();
            _pullAttempt = 0;
        };

        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            _ghostWolfTimer.Reset();
        };

        robotManager.Events.FiniteStateMachineEvents.OnRunState += (Engine engine, State state, CancelEventArgs cancelable) =>
        {
            if (state.DisplayName == "Regeneration")
                _ghostWolfTimer.Reset();
        };

        robotManager.Events.FiniteStateMachineEvents.OnAfterRunState += (Engine engine, State state) =>
        {
            if (state.DisplayName == "Regeneration")
                _ghostWolfTimer.Restart();
        };
            
        Rotation();
    }


    public static void Dispose()
    {
        Main.Log("Stop in progress.");
    }
    
	internal static void Rotation()
	{
        Main.Log("Started");
		while (Main._isLaunched)
		{
			try
			{
				if (!Products.InPause 
                    && !ObjectManager.Me.IsDeadMe 
                    && !Main.HMPrunningAway)
                {
                    if (_goInMelee)
                        Main.SetRangeToMelee();
                    else
                        Main.SetRange(_pullRange);

                    CheckEnchantWeapon();
                    totemManager.CheckForTotemicCall();

                    // Lesser Healing Wave OOC
                    if (!Fight.InFight 
                        && Me.HealthPercent < 65 
                        && LesserHealingWave.KnownSpell
                        && _settings.OOCHeal)
                        Cast(LesserHealingWave);

                    // Ghost Wolf
                    if (Me.ManaPercentage > 50 
                        && !Me.IsIndoors 
                        && _ghostWolfTimer.ElapsedMilliseconds > 3000
                        && _settings.UseGhostWolf 
                        && !Me.IsMounted 
                        && !Fight.InFight 
                        && !Me.HaveBuff("Ghost Wolf")
                        && !ObjectManager.Target.IsFlightMaster)
                    {
                        _ghostWolfTimer.Stop();
                        Cast(GhostWolf);
                    }

                    // Buff rotation
                    if (!Fight.InFight 
                        && ObjectManager.GetNumberAttackPlayer() < 1
                        && !Me.InCombatFlagOnly)
                        BuffRotation();

                    // Pull & Combat rotation
                    if (Fight.InFight 
                        && ObjectManager.Me.Target > 0UL 
                        && ObjectManager.Target.IsAttackable 
                        && ObjectManager.Target.IsAlive)
                    {
                        if (ObjectManager.GetNumberAttackPlayer() < 1 
                            && !ObjectManager.Target.InCombatFlagOnly)
                            Pull();
                        else
                            CombatRotation();
                    }
                }
			}
			catch (Exception arg)
			{
				Logging.WriteError("ERROR: " + arg, true);
			}
			Thread.Sleep(ToolBox.GetLatency() + _settings.ThreadSleepCycle);
		}
        Main.Log("Stopped.");
    }

    internal static void BuffRotation()
    {
        if (!Me.IsMounted && !Me.HaveBuff("Ghost Wolf") && !Me.IsCast)
        {
            // OOC Healing Wave
            if (Me.HealthPercent < 65 
                && !LesserHealingWave.KnownSpell
                && _settings.OOCHeal)
                if (Cast(HealingWave))
                    return;

            // Water Shield
            if (!Me.HaveBuff("Water Shield") 
                && !Me.HaveBuff("Lightning Shield")
                && (_settings.UseWaterShield || !_settings.UseLightningShield || Me.ManaPercentage < 20))
                if (Cast(WaterShield))
                    return;
        }
    }

    internal static void Pull()
    {
        // Melee ?
        if (_pullMeleeTimer.ElapsedMilliseconds <= 0 
            && ObjectManager.Target.GetDistance <= _pullRange + 3)
            _pullMeleeTimer.Start();

        if (_pullMeleeTimer.ElapsedMilliseconds > 8000 
            && !_goInMelee)
        {
            _goInMelee = true;
            _pullMeleeTimer.Reset();
        }

        // Check if caster
        if (_casterEnemies.Contains(ObjectManager.Target.Name))
            _fightingACaster = true;

        // Water Shield
        if (!Me.HaveBuff("Water Shield") 
            && !Me.HaveBuff("Lightning Shield")
            && (_settings.UseWaterShield || !_settings.UseLightningShield) || Me.ManaPercentage < _lowManaThreshold)
            if (Cast(WaterShield))
                return;

        // Ligntning Shield
        if (Me.ManaPercentage > _lowManaThreshold 
            && !Me.HaveBuff("Lightning Shield") 
            && !Me.HaveBuff("Water Shield") 
            && _settings.UseLightningShield 
            && (!WaterShield.KnownSpell || !_settings.UseWaterShield))
            if (Cast(LightningShield))
                return;

        // Pull with Lightning Bolt
        if (ObjectManager.Target.GetDistance <= _pullRange
            && !_goInMelee)
        {
            // pull with rank one
            if (_settings.PullRankOneLightningBolt 
                && LightningBolt.IsSpellUsable)
            {
                MovementManager.StopMove();
                Lua.RunMacroText("/cast Lightning Bolt(Rank 1)");
            }

            // pull with max rank
            if (_settings.PullWithLightningBolt
                && !_settings.PullRankOneLightningBolt
                && LightningBolt.IsSpellUsable)
            {
                MovementManager.StopMove();
                Lua.RunMacroText("/cast Lightning Bolt");
            }

            _pullAttempt++;
            Thread.Sleep(300);

            // Check if we're NOT casting
            if (!Me.IsCast)
            {
                Main.Log($"Pull attempt failed ({_pullAttempt})");
                if (_pullAttempt > 3)
                {
                    Main.Log("Cast unsuccesful, going in melee");
                    _goInMelee = true;
                }
                return;
            }

            // If we're casting
            Usefuls.WaitIsCasting();

            int limit = 1500;
            while (!Me.InCombatFlagOnly && limit > 0)
            {
                Thread.Sleep(100);
                limit -= 100;
            }
        }
    }

    internal static void CombatRotation()
    {
        bool _lowMana = Me.ManaPercentage <= _lowManaThreshold;
        bool _mediumMana = Me.ManaPercentage >= _mediumManaThreshold;
        bool _isPoisoned = ToolBox.HasPoisonDebuff();
        bool _hasDisease = ToolBox.HasDiseaseDebuff();
        bool _shouldBeInterrupted = false;
        WoWUnit Target = ObjectManager.Target;

        // Check Auto-Attacking
        ToolBox.CheckAutoAttack(Attack);

        // Check if we need to interrupt
        int channelTimeLeft = Lua.LuaDoString<int>(@"local spell, _, _, _, endTimeMS = UnitChannelInfo('target')
                                    if spell then
                                     local finish = endTimeMS / 1000 - GetTime()
                                     return finish
                                    end");
        if (channelTimeLeft < 0 || Target.CastingTimeLeft > Usefuls.Latency)
            _shouldBeInterrupted = true;

        // Melee ?
        if (_pullMeleeTimer.ElapsedMilliseconds > 0)
            _pullMeleeTimer.Reset();

        if (_meleeTimer.ElapsedMilliseconds <= 0 
            && !_goInMelee)
            _meleeTimer.Start();

        if ((_shouldBeInterrupted || _meleeTimer.ElapsedMilliseconds > 8000) 
            && !_goInMelee)
        {
            Main.LogDebug("Going in melee range");
            if (!_casterEnemies.Contains(Target.Name))
                _casterEnemies.Add(Target.Name);
            _fightingACaster = true;
            _goInMelee = true;
            _meleeTimer.Stop();
        }

        // Shamanistic Rage
        if (!_mediumMana 
            && ((Target.HealthPercent > 80 && !_settings.ShamanisticRageOnMultiOnly) || ObjectManager.GetNumberAttackPlayer() > 1))
            if (Cast(ShamanisticRage))
                return;

        // Gift of the Naaru
        if (ObjectManager.GetNumberAttackPlayer() > 1 
            && Me.HealthPercent < 50)
            if (Cast(GiftOfTheNaaru))
                return;

        // Blood Fury
        if (Target.HealthPercent > 70)
            if (Cast(BloodFury))
                return;

        // Berserking
        if (Target.HealthPercent > 70)
            if (Cast(Berserking))
                return;

        // Warstomp
        if (ObjectManager.GetNumberAttackPlayer() > 1 
            && Target.GetDistance < 8)
            if (Cast(WarStomp))
                return;

        // Lesser Healing Wave
        if (Me.HealthPercent < 50 
            && LesserHealingWave.KnownSpell 
            && (Target.HealthPercent > 15 || Me.HealthPercent < 25))
            if (Cast(LesserHealingWave))
                return;

        // Healing Wave
        if (Me.HealthPercent < 50 
            && !LesserHealingWave.KnownSpell 
            && (Target.HealthPercent > 15 || Me.HealthPercent < 25))
            if (Cast(HealingWave))
                return;

        // Cure Poison
        if (_isPoisoned && !_lowMana)
        {
            Thread.Sleep(Main._humanReflexTime);
            if (Cast(CurePoison))
                return;
        }

        // Cure Disease
        if (_hasDisease && !_lowMana)
        {
            Thread.Sleep(Main._humanReflexTime);
            if (Cast(CureDisease))
                return;
        }

        // Lightning Shield
        if (!_lowMana && !Me.HaveBuff("Lightning Shield") 
            && !Me.HaveBuff("Water Shield") 
            && _settings.UseLightningShield 
            && (!WaterShield.KnownSpell || !_settings.UseWaterShield))
            if (Cast(LightningShield))
                return;

        // Earth Shock Focused
        if (Me.HaveBuff("Focused")
            && Target.GetDistance < 19f)
            if (Cast(EarthShock))
                return;

        // Frost Shock
        if ((Target.CreatureTypeTarget == "Humanoid" || Target.Name.Contains("Plainstrider"))
            && _settings.FrostShockHumanoids
            && Target.HealthPercent < 40
            && !Target.HaveBuff("Frost Shock"))
            if (Cast(FrostShock))
                return;

        // Earth Shock Interupt Rank 1
        if (_shouldBeInterrupted 
            && Target.GetDistance < 19f 
            && (_settings.InterruptWithRankOne || _lowMana))
        {
            _fightingACaster = true;
            if (!_casterEnemies.Contains(Target.Name))
                _casterEnemies.Add(Target.Name);
            Thread.Sleep(Main._humanReflexTime);
            Lua.RunMacroText("/cast Earth Shock(Rank 1)");
                return;
        }

        // Earth Shock Interupt
        if (_shouldBeInterrupted 
            && Target.GetDistance < 19f 
            && !_settings.InterruptWithRankOne)
        {
            if (!_casterEnemies.Contains(Target.Name))
                _casterEnemies.Add(Target.Name);
            _fightingACaster = true;
            Thread.Sleep(Main._humanReflexTime);
            if (Cast(EarthShock))
                return;
        }

        // Water Shield
        if (!Me.HaveBuff("Water Shield") 
            && !Me.HaveBuff("Lightning Shield")
            && (_settings.UseWaterShield || !_settings.UseLightningShield || _lowMana))
            if (Cast(WaterShield))
                return;

        // Flame Shock DPS
        if (!_lowMana 
            && Target.GetDistance < 19f 
            && !Target.HaveBuff("Flame Shock") 
            && Target.HealthPercent > 20 
            && !_fightingACaster 
            && _settings.UseFlameShock)
            if (Cast(FlameShock))
                return;

        // Totems
        if (!_lowMana 
            && Target.GetDistance < 20)
            if (totemManager.CastTotems())
                return;

        // Stormstrike
        if (!_lowMana 
            && Stormstrike.IsDistanceGood)
            if (Cast(Stormstrike))
                return;

        // Earth Shock DPS
        if (!_lowMana 
            && Target.GetDistance < 19f 
            && !FlameShock.KnownSpell 
            && Target.HealthPercent > 25 
            && Me.ManaPercentage > 30)
            if (Cast(EarthShock))
                return;

        // Low level lightning bolt
        if (!EarthShock.KnownSpell 
            && Me.ManaPercentage > 30 
            && !_lowMana 
            && Target.GetDistance < 29f
            && Target.HealthPercent > 40)
            if (Cast(LightningBolt))
                return;
    }

    public static void ShowConfiguration()
    {
        ZEShamanSettings.Load();
        ZEShamanSettings.CurrentSetting.ToForm();
        ZEShamanSettings.CurrentSetting.Save();
    }

    private static Spell LightningBolt = new Spell("Lightning Bolt");
    private static Spell HealingWave = new Spell("Healing Wave");
    private static Spell LesserHealingWave = new Spell("Lesser Healing Wave");
    private static Spell RockbiterWeapon = new Spell("Rockbiter Weapon");
    private static Spell EarthShock = new Spell("Earth Shock");
    private static Spell FlameShock = new Spell("Flame Shock");
    private static Spell FrostShock = new Spell("Frost Shock");
    private static Spell LightningShield = new Spell("Lightning Shield");
    private static Spell WaterShield = new Spell("Water Shield");
    private static Spell GhostWolf = new Spell("Ghost Wolf");
    private static Spell CurePoison = new Spell("Cure Poison");
    private static Spell CureDisease = new Spell("Cure Disease");
    private static Spell WindfuryWeapon = new Spell("Windfury Weapon");
    private static Spell Stormstrike = new Spell("Stormstrike");
    private static Spell ShamanisticRage = new Spell("Shamanistic Rage");
    private static Spell Attack = new Spell("Attack");
    private static Spell BloodFury = new Spell("Blood Fury");
    private static Spell Berserking = new Spell("Berserking");
    private static Spell WarStomp = new Spell("War Stomp");
    private static Spell GiftOfTheNaaru = new Spell("Gift of the Naaru");

    internal static bool Cast(Spell s)
    {
        CombatDebug("In cast for " + s.Name);
        if (!s.IsSpellUsable || !s.KnownSpell || Me.IsCast)
            return false;
        
        s.Launch();
        Usefuls.WaitIsCasting();
        return true;
    }

    private static void CombatDebug(string s)
    {
        if (_settings.ActivateCombatDebug)
            Main.CombatDebug(s);
    }

    private static void CheckEnchantWeapon()
    {
        bool hasMainHandEnchant = Lua.LuaDoString<bool>
            (@"local hasMainHandEnchant, _, _, _, _, _, _, _, _ = GetWeaponEnchantInfo()
            if (hasMainHandEnchant) then 
               return '1'
            else
               return '0'
            end");

        bool hasOffHandEnchant = Lua.LuaDoString<bool>
            (@"local _, _, _, _, hasOffHandEnchant, _, _, _, _ = GetWeaponEnchantInfo()
            if (hasOffHandEnchant) then 
               return '1'
            else
               return '0'
            end");

        bool hasoffHandWeapon = Lua.LuaDoString<bool>(@"local hasWeapon = OffhandHasWeapon()
            return hasWeapon");

        if (!hasMainHandEnchant || (hasoffHandWeapon && !hasOffHandEnchant))
        {
            if (!WindfuryWeapon.KnownSpell && RockbiterWeapon.KnownSpell)
                Cast(RockbiterWeapon);

            if (WindfuryWeapon.KnownSpell)
                Cast(WindfuryWeapon);
        }
    }
}
