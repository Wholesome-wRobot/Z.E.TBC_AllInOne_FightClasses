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
using WholesomeTBCAIO.Settings;
using WholesomeTBCAIO.Helpers;

namespace WholesomeTBCAIO.Rotations.Warrior
{
    public class Warrior : IClassRotation
    {
        public static WarriorSettings settings;

        protected Stopwatch _pullMeleeTimer = new Stopwatch();
        protected Stopwatch _meleeTimer = new Stopwatch();
        protected WoWLocalPlayer Me = ObjectManager.Me;

        protected float _pullRange = 25f;
        protected bool _fightingACaster = false;
        protected List<string> _casterEnemies = new List<string>();
        protected bool _pullFromAfar = false;

        protected Warrior specialization;

        public void Initialize(IClassRotation specialization)
        {
            Logger.Log("Initialized");
            settings = WarriorSettings.Current;

            this.specialization = specialization as Warrior;
            Talents.InitTalents(settings);

            FightEvents.OnFightEnd += (guid) =>
            {
                _fightingACaster = false;
                _meleeTimer.Reset();
                _pullMeleeTimer.Reset();
                _pullFromAfar = false;
                RangeManager.SetRange(RangeManager.DefaultMeleeRange);
            };

            Rotation();
        }

        public void Dispose()
        {
            Logger.Log("Stop in progress.");
        }

        private void Rotation()
        {
            Logger.Log("Started");
            while (Main.isLaunched)
            {
                try
                {
                    if (!Products.InPause
                        && !ObjectManager.Me.IsDeadMe
                        && !Main.HMPrunningAway)
                    {
                        // Buff rotation
                        if (!Fight.InFight
                            && ObjectManager.GetNumberAttackPlayer() < 1)
                        {
                            specialization.BuffRotation();
                        }

                        // Pull & Combat rotation
                        if (Fight.InFight
                            && ObjectManager.Me.Target > 0UL
                            && ObjectManager.Target.IsAttackable
                            && ObjectManager.Target.IsAlive)
                        {
                            if (ObjectManager.GetNumberAttackPlayer() < 1
                                && !ObjectManager.Target.InCombatFlagOnly)
                                specialization.Pull();
                            else
                                specialization.CombatRotation();
                        }
                    }
                }
                catch (Exception arg)
                {
                    Logging.WriteError("ERROR: " + arg, true);
                }
                Thread.Sleep(ToolBox.GetLatency() + settings.ThreadSleepCycle);
            }
            Logger.Log("Stopped.");
        }

        protected virtual void BuffRotation()
        {
            if (!Me.IsMounted && !Me.IsCast)
            {
                // Battle Shout
                if (!Me.HaveBuff("Battle Shout")
                    && BattleShout.IsSpellUsable &&
                    (!settings.UseCommandingShout || !CommandingShout.KnownSpell))
                    if (Cast(BattleShout))
                        return;

                // Commanding Shout
                if (!Me.HaveBuff("Commanding Shout")
                    && settings.UseCommandingShout
                    && CommandingShout.KnownSpell)
                    if (Cast(CommandingShout))
                        return;

                // Cannibalize
                if (ObjectManager.GetObjectWoWUnit().Where(u => u.GetDistance <= 8
                && u.IsDead
                && (u.CreatureTypeTarget == "Humanoid" || u.CreatureTypeTarget == "Undead")).Count() > 0)
                {
                    if (Me.HealthPercent < 50
                        && !Me.HaveBuff("Drink")
                        && !Me.HaveBuff("Food")
                        && Me.IsAlive
                        && Cannibalize.KnownSpell
                        && Cannibalize.IsSpellUsable)
                        if (Cast(Cannibalize))
                            return;
                }
            }
        }

        protected virtual void Pull()
        {
            RangeManager.SetRangeToMelee();

            // Check if surrounding enemies
            if (ObjectManager.Target.GetDistance < _pullRange && !_pullFromAfar)
                _pullFromAfar = ToolBox.CheckIfEnemiesOnPull(ObjectManager.Target, _pullRange);

            // Check stance
            if (!InBattleStance()
                && ObjectManager.Me.Rage < 10
                && !_pullFromAfar
                && !settings.AlwaysPull)
                Cast(BattleStance);

            // Pull from afar
            if (_pullFromAfar
                && _pullMeleeTimer.ElapsedMilliseconds < 5000 || settings.AlwaysPull
                && ObjectManager.Target.GetDistance < 24f)
            {
                Spell pullMethod = null;

                if (Shoot.IsSpellUsable
                    && Shoot.KnownSpell)
                    pullMethod = Shoot;

                if (Throw.IsSpellUsable
                    && Throw.KnownSpell)
                    pullMethod = Throw;

                if (pullMethod == null)
                {
                    Logger.Log("Can't pull from distance. Please equip a ranged weapon in order to Throw or Shoot.");
                    _pullFromAfar = false;
                }
                else
                {
                    if (Me.IsMounted)
                        MountTask.DismountMount();

                    RangeManager.SetRange(_pullRange);
                    if (Cast(pullMethod))
                        Thread.Sleep(2000);
                }
            }

            // Melee ?
            if (_pullMeleeTimer.ElapsedMilliseconds <= 0
                && ObjectManager.Target.GetDistance <= _pullRange + 3)
                _pullMeleeTimer.Start();

            if (_pullMeleeTimer.ElapsedMilliseconds > 5000)
            {
                Logger.LogDebug("Going in Melee range");
                RangeManager.SetRangeToMelee();
                _pullMeleeTimer.Reset();
            }

            // Check if caster in list
            if (_casterEnemies.Contains(ObjectManager.Target.Name))
                _fightingACaster = true;

            // Charge Battle Stance
            if (InBattleStance()
                && ObjectManager.Target.GetDistance > 9f
                && ObjectManager.Target.GetDistance < 24f
                && !_pullFromAfar)
                if (Cast(Charge))
                    return;

            // Charge Berserker Stance
            if (InBerserkStance()
                && ObjectManager.Target.GetDistance > 9f
                && ObjectManager.Target.GetDistance < 24f
                && !_pullFromAfar)
                if (Cast(Intercept))
                    return;
        }

        protected virtual void CombatRotation()
        {
            WoWUnit Target = ObjectManager.Target;
            bool _shouldBeInterrupted = ToolBox.EnemyCasting();
            bool _inMeleeRange = Target.GetDistance < 6f;
            bool _saveRage = Cleave.KnownSpell
                && ObjectManager.GetNumberAttackPlayer() > 1
                && ToolBox.CheckIfEnemiesClose(15f)
                && settings.UseCleave
                || Execute.KnownSpell && Target.HealthPercent < 40
                || Bloodthirst.KnownSpell && ObjectManager.Me.Rage < 40 && Target.HealthPercent > 50;

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

            if (_meleeTimer.ElapsedMilliseconds <= 0
                && _pullFromAfar)
                _meleeTimer.Start();

            if ((_shouldBeInterrupted || _meleeTimer.ElapsedMilliseconds > 5000)
                && !RangeManager.CurrentRangeIsMelee())
            {
                Logger.LogDebug("Going in Melee range 2");
                RangeManager.SetRangeToMelee();
                _meleeTimer.Stop();
            }

            // Gift of the Naaru
            if (ObjectManager.GetNumberAttackPlayer() > 1
                && Me.HealthPercent < 50)
                if (Cast(GiftOfTheNaaru))
                    return;

            // Will of the Forsaken
            if (Me.HaveBuff("Fear")
                || Me.HaveBuff("Charm")
                || Me.HaveBuff("Sleep"))
                if (Cast(WillOfTheForsaken))
                    return;

            // Stoneform
            if (ToolBox.HasPoisonDebuff()
                || ToolBox.HasDiseaseDebuff()
                || Me.HaveBuff("Bleed"))
                if (Cast(Stoneform))
                    return;

            // Escape Artist
            if (Me.Rooted
                || Me.HaveBuff("Frostnova"))
                if (Cast(EscapeArtist))
                    return;

            // Warstomp
            if (ObjectManager.GetNumberAttackPlayer() > 1
                && Target.GetDistance < 8)
                if (Cast(WarStomp))
                    return;

            // Intercept
            if (InBerserkStance()
                && ObjectManager.Target.GetDistance > 12f
                && ObjectManager.Target.GetDistance < 24f)
                if (Cast(Intercept))
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
            if (InBerserkStance() && Me.Rage < 10
                && (!settings.PrioritizeBerserkStance || ObjectManager.GetNumberAttackPlayer() > 1)
                && !_fightingACaster)
                if (Cast(BattleStance))
                    return;

            // Berserker stance
            if (settings.PrioritizeBerserkStance
                && !InBerserkStance()
                && BerserkerStance.KnownSpell
                && Me.Rage < 15
                && ObjectManager.GetNumberAttackPlayer() < 2)
                if (Cast(BerserkerStance))
                    return;

            // Fighting a caster
            if (_fightingACaster
                && !InBerserkStance()
                && BerserkerStance.KnownSpell
                && Me.Rage < 20
                && ObjectManager.GetNumberAttackPlayer() < 2)
            {
                if (Cast(BerserkerStance))
                    return;
            }

            // Interrupt
            if (_shouldBeInterrupted && InBerserkStance())
            {
                Thread.Sleep(Main.humanReflexTime);
                if (Cast(Pummel))
                    return;
            }

            // Victory Rush
            if (VictoryRush.KnownSpell)
                if (Cast(VictoryRush))
                    return;

            // Rampage
            if (Rampage.KnownSpell
                && (!Me.HaveBuff("Rampage") || Me.HaveBuff("Rampage") && ToolBox.BuffTimeLeft("Rampage") < 10))
                if (Cast(Rampage))
                    return;

            // Berserker Rage
            if (InBerserkStance()
                && Target.HealthPercent > 70)
                if (Cast(BerserkerRage))
                    return;

            // Execute
            if (Target.HealthPercent < 20)
                if (Cast(Execute))
                    return;

            // Overpower
            if (Overpower.IsSpellUsable)
            {
                Thread.Sleep(Main.humanReflexTime);
                if (Cast(Overpower))
                    return;
            }

            // Bloodthirst
            if (Cast(Bloodthirst))
                return;

            // Sweeping Strikes
            if (_inMeleeRange
                && ObjectManager.GetNumberAttackPlayer() > 1
                && ToolBox.CheckIfEnemiesClose(15f))
                if (Cast(SweepingStrikes))
                    return;

            // Retaliation
            if (_inMeleeRange && ObjectManager.GetNumberAttackPlayer() > 1
                && ToolBox.CheckIfEnemiesClose(15f))
                if (Cast(Retaliation) && (!SweepingStrikes.IsSpellUsable || !SweepingStrikes.KnownSpell))
                    return;

            // Cleave
            if (_inMeleeRange
                && ObjectManager.GetNumberAttackPlayer() > 1
                && ToolBox.CheckIfEnemiesClose(15f) &&
                (!SweepingStrikes.IsSpellUsable || !SweepingStrikes.KnownSpell) && ObjectManager.Me.Rage > 40
                && settings.UseCleave)
                if (Cast(Cleave))
                    return;

            // Blood Rage
            if (settings.UseBloodRage
                && Me.HealthPercent > 90)
                if (Cast(BloodRage))
                    return;

            // Hamstring
            if ((Target.CreatureTypeTarget == "Humanoid" || Target.Name.Contains("Plainstrider"))
                && _inMeleeRange
                && settings.UseHamstring
                && Target.HealthPercent < 40
                && !Target.HaveBuff("Hamstring"))
                if (Cast(Hamstring))
                    return;

            // Commanding Shout
            if (!Me.HaveBuff("Commanding Shout")
                && settings.UseCommandingShout
                && CommandingShout.KnownSpell)
                if (Cast(CommandingShout))
                    return;

            // Battle Shout
            if (!Me.HaveBuff("Battle Shout")
                && (!settings.UseCommandingShout || !CommandingShout.KnownSpell))
                if (Cast(BattleShout))
                    return;

            // Rend
            if (!Target.HaveBuff("Rend")
                && ToolBox.CanBleed(Target)
                && _inMeleeRange
                && settings.UseRend
                && Target.HealthPercent > 25)
                if (Cast(Rend))
                    return;

            // Demoralizing Shout
            if (settings.UseDemoralizingShout
                && !Target.HaveBuff("Demoralizing Shout")
                && (ObjectManager.GetNumberAttackPlayer() > 1 || !ToolBox.CheckIfEnemiesClose(15f)) && _inMeleeRange)
                if (Cast(DemoralizingShout))
                    return;

            // Heroic Strike
            if (_inMeleeRange
                && !HeroicStrikeOn()
                && (!_saveRage || Me.Rage > 60))
                if (Cast(HeroicStrike))
                    return;
        }

        protected Spell Attack = new Spell("Attack");
        protected Spell HeroicStrike = new Spell("Heroic Strike");
        protected Spell BattleShout = new Spell("Battle Shout");
        protected Spell CommandingShout = new Spell("Commanding Shout");
        protected Spell Charge = new Spell("Charge");
        protected Spell Rend = new Spell("Rend");
        protected Spell Hamstring = new Spell("Hamstring");
        protected Spell BloodRage = new Spell("Bloodrage");
        protected Spell Overpower = new Spell("Overpower");
        protected Spell DemoralizingShout = new Spell("Demoralizing Shout");
        protected Spell Throw = new Spell("Throw");
        protected Spell Shoot = new Spell("Shoot");
        protected Spell Retaliation = new Spell("Retaliation");
        protected Spell Cleave = new Spell("Cleave");
        protected Spell Execute = new Spell("Execute");
        protected Spell SweepingStrikes = new Spell("Sweeping Strikes");
        protected Spell Bloodthirst = new Spell("Bloodthirst");
        protected Spell BerserkerStance = new Spell("Berserker Stance");
        protected Spell BattleStance = new Spell("Battle Stance");
        protected Spell Intercept = new Spell("Intercept");
        protected Spell Pummel = new Spell("Pummel");
        protected Spell BerserkerRage = new Spell("Berserker Rage");
        protected Spell Rampage = new Spell("Rampage");
        protected Spell VictoryRush = new Spell("Victory Rush");
        protected Spell Cannibalize = new Spell("Cannibalize");
        protected Spell WillOfTheForsaken = new Spell("Will of the Forsaken");
        protected Spell BloodFury = new Spell("Blood Fury");
        protected Spell Berserking = new Spell("Berserking");
        protected Spell WarStomp = new Spell("War Stomp");
        protected Spell Stoneform = new Spell("Stoneform");
        protected Spell EscapeArtist = new Spell("Escape Artist");
        protected Spell GiftOfTheNaaru = new Spell("Gift of the Naaru");

        protected bool Cast(Spell s)
        {
            CombatDebug("In cast for " + s.Name);
            if (!s.IsSpellUsable || !s.KnownSpell || Me.IsCast)
                return false;

            s.Launch();
            return true;
        }

        protected void CombatDebug(string s)
        {
            if (settings.ActivateCombatDebug)
                Logger.CombatDebug(s);
        }

        protected bool HeroicStrikeOn()
        {
            return Lua.LuaDoString<bool>("hson = false; if IsCurrentSpell('Heroic Strike') then hson = true end", "hson");
        }

        protected bool InBattleStance()
        {
            return Lua.LuaDoString<bool>("bs = false; if GetShapeshiftForm() == 1 then bs = true end", "bs");
        }

        protected bool InBerserkStance()
        {
            return Lua.LuaDoString<bool>("bs = false; if GetShapeshiftForm() == 3 then bs = true end", "bs");
        }
    }
}