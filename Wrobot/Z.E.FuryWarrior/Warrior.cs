using System;
using System.Diagnostics;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.Collections.Generic;
using wManager.Wow.Bot.Tasks;
using System.Linq;

public static class Warrior
{
    private static float _meleRange = Main.settingRange;
    private static float _pullRange = 25f;
    internal static Stopwatch _pullMeleeTimer = new Stopwatch();
    internal static Stopwatch _meleeTimer = new Stopwatch();
    private static WoWLocalPlayer Me = ObjectManager.Me;
    internal static ZEWarriorSettings _settings;
    private static bool _fightingACaster = false;
    private static List<string> _casterEnemies = new List<string>();
    private static bool _pullFromAfar = false;

    public static void Initialize()
    {
        Main.Log("Initialized");
        ZEWarriorSettings.Load();
        _settings = ZEWarriorSettings.CurrentSetting;
        Talents.InitTalents(_settings.AssignTalents, _settings.UseDefaultTalents, _settings.TalentCodes);

        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _fightingACaster = false;
            _meleeTimer.Reset();
            _pullMeleeTimer.Reset();
            _pullFromAfar = false;
            Main.settingRange = _meleRange;
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
				if (!Products.InPause && !ObjectManager.Me.IsDeadMe && !Main.HMPrunningAway)
                {
                    // Buff rotation
                    if (!Fight.InFight && ObjectManager.GetNumberAttackPlayer() < 1)
                    {
                        BuffRotation();
                    }

                    // Pull & Combat rotation
                    if (Fight.InFight && ObjectManager.Me.Target > 0UL && ObjectManager.Target.IsAttackable && ObjectManager.Target.IsAlive)
                    {
                        if (ObjectManager.GetNumberAttackPlayer() < 1 && !ObjectManager.Target.InCombatFlagOnly)
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
        if (!Me.IsMounted && !Me.IsCast)
        {
            // Battle Shout
            if (!Me.HaveBuff("Battle Shout") && BattleShout.IsSpellUsable && 
                (!_settings.UseCommandingShout || !CommandingShout.KnownSpell))
                if (Cast(BattleShout))
                    return;

            // Commanding Shout
            if (!Me.HaveBuff("Commanding Shout") && (_settings.UseCommandingShout && CommandingShout.KnownSpell))
                if (Cast(CommandingShout))
                    return;

            // Cannibalize
            if (ObjectManager.GetObjectWoWUnit().Where(u => u.GetDistance <= 8 && u.IsDead && (u.CreatureTypeTarget == "Humanoid" || u.CreatureTypeTarget == "Undead")).Count() > 0)
            {
                if (Me.HealthPercent < 50 && !Me.HaveBuff("Drink") && !Me.HaveBuff("Food") && Me.IsAlive && Cannibalize.KnownSpell && Cannibalize.IsSpellUsable)
                    if (Cast(Cannibalize))
                        return;
            }
        }
    }

    internal static void Pull()
    {
        // Check if surrounding enemies
        if (ObjectManager.Target.GetDistance < _pullRange && !_pullFromAfar)
            _pullFromAfar = ToolBox.CheckIfEnemiesOnPull(ObjectManager.Target, _pullRange);

        // Check stance
        if (!InBattleStance() && ObjectManager.Me.Rage < 10 && !_pullFromAfar && !_settings.AlwaysPull)
            Cast(BattleStance);

        // Pull from afar
        if ((_pullFromAfar && _pullMeleeTimer.ElapsedMilliseconds < 5000) || _settings.AlwaysPull
            && ObjectManager.Target.GetDistance < 24f)
        {
            Spell pullMethod = null;

            if (Shoot.IsSpellUsable && Shoot.KnownSpell)
                pullMethod = Shoot;

            if (Throw.IsSpellUsable && Throw.KnownSpell)
                pullMethod = Throw;

            if (pullMethod == null)
            {
                Main.Log("Can't pull from distance. Please equip a ranged weapon in order to Throw or Shoot.");
                _pullFromAfar = false;
            }
            else
            {
                if (Me.IsMounted)
                    MountTask.DismountMount();

                Main.settingRange = _pullRange;
                if (Cast(pullMethod))
                    Thread.Sleep(2000);
            }
        }

        // Melee ?
        if (_pullMeleeTimer.ElapsedMilliseconds <= 0 && ObjectManager.Target.GetDistance <= _pullRange + 3)
            _pullMeleeTimer.Start();

        if (_pullMeleeTimer.ElapsedMilliseconds > 5000)
        {
            Main.LogDebug("Going in Melee range");
            Main.settingRange = _meleRange;
            _pullMeleeTimer.Reset();
        }

        // Check if caster in list
        if (_casterEnemies.Contains(ObjectManager.Target.Name))
            _fightingACaster = true;

        // Charge Battle Stance
        if (InBattleStance() && ObjectManager.Target.GetDistance > 9f && ObjectManager.Target.GetDistance < 24f 
            && !_pullFromAfar)
            if (Cast(Charge))
                return;

        // Charge Berserker Stance
        if (InBerserkStance() && ObjectManager.Target.GetDistance > 9f && ObjectManager.Target.GetDistance < 24f 
            && !_pullFromAfar)
            if (Cast(Intercept))
                return;
    }

    internal static void CombatRotation()
    {
        WoWUnit Target = ObjectManager.Target;
        bool _shouldBeInterrupted = ToolBox.EnemyCasting();
        bool _inMeleeRange = Target.GetDistance < 6f;
        bool _saveRage = ((Cleave.KnownSpell && ObjectManager.GetNumberAttackPlayer() > 1 && ToolBox.CheckIfEnemiesClose(15f)
            && _settings.UseCleave)
            || (Execute.KnownSpell && Target.HealthPercent < 40) 
            || (Bloodthirst.KnownSpell && ObjectManager.Me.Rage < 40 && Target.HealthPercent > 50));

        // Check Auto-Attacking
        ToolBox.CheckAutoAttack(Attack);

        // Check if we need to interrupt
        if (_shouldBeInterrupted)
        {
            _fightingACaster = true;
            if (!_casterEnemies.Contains(Target.Name))
                _casterEnemies.Add(Target.Name);
        }

        // Melee ?
        if (_pullMeleeTimer.ElapsedMilliseconds > 0)
            _pullMeleeTimer.Reset();

        if (_meleeTimer.ElapsedMilliseconds <= 0 && _pullFromAfar)
            _meleeTimer.Start();

        if ((_shouldBeInterrupted || _meleeTimer.ElapsedMilliseconds > 5000) && Main.settingRange != _meleRange)
        {
            Main.LogDebug("Going in Melee range 2");
            Main.settingRange = _meleRange;
            _meleeTimer.Stop();
        }

        // Gift of the Naaru
        if (ObjectManager.GetNumberAttackPlayer() > 1 && Me.HealthPercent < 50)
            if (Cast(GiftOfTheNaaru))
                return;

        // Will of the Forsaken
        if (Me.HaveBuff("Fear") || Me.HaveBuff("Charm") || Me.HaveBuff("Sleep"))
            if (Cast(WillOfTheForsaken))
                return;

        // Stoneform
        if (ToolBox.HasPoisonDebuff() || ToolBox.HasDiseaseDebuff() || Me.HaveBuff("Bleed"))
            if (Cast(Stoneform))
                return;

        // Escape Artist
        if (Me.Rooted || Me.HaveBuff("Frostnova"))
            if (Cast(EscapeArtist))
                return;

        // Warstomp
        if (ObjectManager.GetNumberAttackPlayer() > 1 && Target.GetDistance < 8)
            if (Cast(WarStomp))
                return;

        // Blood Fury
        if (Target.HealthPercent > 70)
            if (Cast(BloodFury))
                return;

        // Berserking
        if (Target.HealthPercent > 70)
            if (Cast(Berserking))
                return;

        // Battle stance
        if (InBerserkStance() && Me.Rage < 10 && (!_settings.PrioritizeBerserkStance || ObjectManager.GetNumberAttackPlayer() > 1) 
            && !_fightingACaster)
            if (Cast(BattleStance))
                return;

        // Berserker stance
        if (_settings.PrioritizeBerserkStance && !InBerserkStance() && BerserkerStance.KnownSpell && Me.Rage < 15
            && ObjectManager.GetNumberAttackPlayer() < 2)
            if (Cast(BerserkerStance))
                return;

        // Fighting a caster
        if (_fightingACaster && !InBerserkStance() && BerserkerStance.KnownSpell && Me.Rage < 20
            && ObjectManager.GetNumberAttackPlayer() < 2)
        {
            if (Cast(BerserkerStance))
                return;
        }

        // Interrupt
        if (_shouldBeInterrupted && InBerserkStance())
        {
            Thread.Sleep(Main._humanReflexTime);
            if (Cast(Pummel))
                return;
        }

        // Victory Rush
        if (VictoryRush.KnownSpell)
            if (Cast(VictoryRush))
                return;

        // Rampage
        if (Rampage.KnownSpell && (!Me.HaveBuff("Rampage") || (Me.HaveBuff("Rampage") && ToolBox.BuffTimeLeft("Rampage") < 10)))
            if (Cast(Rampage))
                return;

        // Berserker Rage
        if (InBerserkStance() && Target.HealthPercent > 70)
            if (Cast(BerserkerRage))
                return;

        // Execute
        if (Target.HealthPercent < 20)
            if (Cast(Execute))
                return;

        // Overpower
        if (Overpower.IsSpellUsable)
        {
            Thread.Sleep(Main._humanReflexTime);
            if (Cast(Overpower))
                return;
        }

        // Bloodthirst
        if (Cast(Bloodthirst))
            return;

        // Sweeping Strikes
        if (_inMeleeRange && ObjectManager.GetNumberAttackPlayer() > 1 && ToolBox.CheckIfEnemiesClose(15f))
            if (Cast(SweepingStrikes))
                return;

        // Retaliation
        if (_inMeleeRange && ObjectManager.GetNumberAttackPlayer() > 1 && ToolBox.CheckIfEnemiesClose(15f))
            if (Cast(Retaliation) && (!SweepingStrikes.IsSpellUsable || !SweepingStrikes.KnownSpell))
                return;

        // Cleave
        if (_inMeleeRange && ObjectManager.GetNumberAttackPlayer() > 1 && ToolBox.CheckIfEnemiesClose(15f) && 
            (!SweepingStrikes.IsSpellUsable || !SweepingStrikes.KnownSpell) && ObjectManager.Me.Rage > 40
            && _settings.UseCleave)
            if (Cast(Cleave))
                return;

        // Blood Rage
        if (_settings.UseBloodRage && Me.HealthPercent > 90)
            if (Cast(BloodRage))
                return;

        // Hamstring
        if (Target.CreatureTypeTarget == "Humanoid" && _inMeleeRange && _settings.UseHamstring && Target.HealthPercent < 40
            && !Target.HaveBuff("Hamstring"))
            if (Cast(Hamstring))
                return;

        // Commanding Shout
        if (!Me.HaveBuff("Commanding Shout") && (_settings.UseCommandingShout && CommandingShout.KnownSpell))
            if (Cast(CommandingShout))
                return;

        // Battle Shout
        if (!Me.HaveBuff("Battle Shout") && (!_settings.UseCommandingShout || !CommandingShout.KnownSpell))
            if (Cast(BattleShout))
                return;

        // Rend
        if (!Target.HaveBuff("Rend") && ToolBox.CanBleed(Target) && _inMeleeRange && _settings.UseRend
            && Target.HealthPercent > 25)
            if (Cast(Rend))
                return;

        // Demoralizing Shout
        if (_settings.UseDemoralizingShout && !Target.HaveBuff("Demoralizing Shout") 
            && (ObjectManager.GetNumberAttackPlayer() > 1 || !ToolBox.CheckIfEnemiesClose(15f)) && _inMeleeRange)
            if (Cast(DemoralizingShout))
                return;
        
        // Heroic Strike
        if (_inMeleeRange && !HeroicStrikeOn() && (!_saveRage || Me.Rage > 60))
            if (Cast(HeroicStrike))
                return;
    }

    public static void ShowConfiguration()
    {
        ZEWarriorSettings.Load();
        ZEWarriorSettings.CurrentSetting.ToForm();
        ZEWarriorSettings.CurrentSetting.Save();
    }

    private static Spell Attack = new Spell("Attack");
    private static Spell HeroicStrike = new Spell("Heroic Strike");
    private static Spell BattleShout = new Spell("Battle Shout");
    private static Spell CommandingShout = new Spell("Commanding Shout");
    private static Spell Charge = new Spell("Charge");
    private static Spell Rend = new Spell("Rend");
    private static Spell Hamstring = new Spell("Hamstring");
    private static Spell BloodRage = new Spell("Bloodrage");
    private static Spell Overpower = new Spell("Overpower");
    private static Spell DemoralizingShout = new Spell("Demoralizing Shout");
    private static Spell Throw = new Spell("Throw");
    private static Spell Shoot = new Spell("Shoot");
    private static Spell Retaliation = new Spell("Retaliation");
    private static Spell Cleave = new Spell("Cleave");
    private static Spell Execute = new Spell("Execute");
    private static Spell SweepingStrikes = new Spell("Sweeping Strikes");
    private static Spell Bloodthirst = new Spell("Bloodthirst");
    private static Spell BerserkerStance = new Spell("Berserker Stance");
    private static Spell BattleStance = new Spell("Battle Stance");
    private static Spell Intercept = new Spell("Intercept");
    private static Spell Pummel = new Spell("Pummel");
    private static Spell BerserkerRage = new Spell("Berserker Rage");
    private static Spell Rampage = new Spell("Rampage");
    private static Spell VictoryRush = new Spell("Victory Rush");
    private static Spell Cannibalize = new Spell("Cannibalize");
    private static Spell WillOfTheForsaken = new Spell("Will of the Forsaken");
    private static Spell BloodFury = new Spell("Blood Fury");
    private static Spell Berserking = new Spell("Berserking");
    private static Spell WarStomp = new Spell("War Stomp");
    private static Spell Stoneform = new Spell("Stoneform");
    private static Spell EscapeArtist = new Spell("Escape Artist");
    private static Spell GiftOfTheNaaru = new Spell("Gift of the Naaru");

    internal static bool Cast(Spell s)
    {
        CombatDebug("In cast for " + s.Name);
        if (!s.IsSpellUsable || !s.KnownSpell || Me.IsCast)
            return false;
        
        s.Launch();
        return true;
    }

    private static void CombatDebug(string s)
    {
        if (_settings.ActivateCombatDebug)
            Main.CombatDebug(s);
    }

    private static bool HeroicStrikeOn()
    {
        return Lua.LuaDoString<bool>("hson = false; if IsCurrentSpell('Heroic Strike') then hson = true end", "hson");
    }

    private static bool InBattleStance()
    {
        return Lua.LuaDoString<bool>("bs = false; if GetShapeshiftForm() == 1 then bs = true end", "bs");
    }

    private static bool InBerserkStance()
    {
        return Lua.LuaDoString<bool>("bs = false; if GetShapeshiftForm() == 3 then bs = true end", "bs");
    }
}
