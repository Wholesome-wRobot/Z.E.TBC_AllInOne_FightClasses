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
using System.ComponentModel;
using System.Linq;
using WholesomeTBCAIO.Settings;
using WholesomeTBCAIO.Helpers;

namespace WholesomeTBCAIO.Rotations.Rogue
{
    public class Rogue : IClassRotation
    {
        public static RogueSettings settings;

        protected Stopwatch _pullMeleeTimer = new Stopwatch();
        protected Stopwatch _meleeTimer = new Stopwatch();
        protected Stopwatch _stealthApproachTimer = new Stopwatch();
        protected WoWLocalPlayer Me = ObjectManager.Me;
        protected List<string> _casterEnemies = new List<string>();

        protected readonly BackgroundWorker _pulseThread = new BackgroundWorker();

        protected float _pullRange = 25f;
        protected bool _fightingACaster = false;
        protected bool _pullFromAfar = false;
        protected bool _isStealthApproching;
        public static uint MHPoison;
        public static uint OHPoison;
        protected string _myBestBandage = null;

        protected Rogue specialization;

        public void Initialize(IClassRotation specialization)
        {
            Logger.Log("Initialized");
            settings = RogueSettings.Current;

            this.specialization = specialization as Rogue;
            Talents.InitTalents(settings);

            RangeManager.SetRangeToMelee();

            // Fight End
            FightEvents.OnFightEnd += (guid) =>
            {
                _meleeTimer.Reset();
                _pullMeleeTimer.Reset();
                _stealthApproachTimer.Reset();
                _fightingACaster = false;
                _pullFromAfar = false;
                _isStealthApproching = false;
                _myBestBandage = null;
                RangeManager.SetRangeToMelee();
            };

            // Fight Start
            FightEvents.OnFightStart += (unit, cancelable) =>
            {
                _myBestBandage = ToolBox.GetBestMatchingItem(Bandages());
                if (_myBestBandage != null)
                    Logger.LogDebug("Found best bandage : " + _myBestBandage);
            };

            // We override movement to target when approaching in Stealth
            MovementEvents.OnMoveToPulse += (point, cancelable) =>
            {
                if (_isStealthApproching &&
                !point.ToString().Equals(ToolBox.BackofVector3(ObjectManager.Target.Position, ObjectManager.Target, 2.5f).ToString()))
                    cancelable.Cancel = true;
            };

            // Fight Loop - Go behind target when gouged
            FightEvents.OnFightLoop += (unit, cancelable) =>
            {
                if (IsTargetStunned() && !MovementManager.InMovement && Me.IsAlive && !Me.IsCast)
                {
                    if (Me.IsAlive && ObjectManager.Target.IsAlive)
                    {
                        Vector3 position = ToolBox.BackofVector3(ObjectManager.Target.Position, ObjectManager.Target, 2.5f);
                        MovementManager.Go(PathFinder.FindPath(position), false);

                        while (MovementManager.InMovement && Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                        && IsTargetStunned())
                        {
                            // Wait follow path
                            Thread.Sleep(500);
                        }
                    }
                }
            };

            // BL Hook
            OthersEvents.OnAddBlackListGuid += (guid, timeInMilisec, isSessionBlacklist, cancelable) =>
            {
                if (Me.HaveBuff("Stealth") && !_pullFromAfar)
                {
                    Logger.LogDebug("BL : " + guid + " ms : " + timeInMilisec + " is session: " + isSessionBlacklist);
                    Logger.Log("Cancelling Blacklist event");
                    cancelable.Cancel = true;
                }
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
                        if (ObjectManager.GetNumberAttackPlayer() < 1)
                        {
                            specialization.BuffRotation();
                        }

                        // Pull & Combat rotation
                        if (Fight.InFight && ObjectManager.Me.Target > 0UL && ObjectManager.Target.IsAttackable
                            && ObjectManager.Target.IsAlive)
                        {
                            if (ObjectManager.GetNumberAttackPlayer() < 1 && !ObjectManager.Target.InCombatFlagOnly)
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
                PoisonWeapon();

                // Sprint
                if (settings.SprintWhenAvail && Me.HealthPercent > 80 && MovementManager.InMovement && Sprint.IsSpellUsable
                    && Sprint.KnownSpell)
                    Sprint.Launch();

                // Cannibalize
                if (ObjectManager.GetObjectWoWUnit().Where(u => u.GetDistance <= 8 && u.IsDead && (u.CreatureTypeTarget == "Humanoid" || u.CreatureTypeTarget == "Undead")).Count() > 0)
                {
                    if (Me.HealthPercent < 50 && !Me.HaveBuff("Drink") && !Me.HaveBuff("Food") && Me.IsAlive && Cannibalize.KnownSpell && Cannibalize.IsSpellUsable)
                        if (Cast(Cannibalize))
                            return;
                }
            }
        }

        protected virtual void Pull()
        {
            // Check if surrounding enemies
            if (ObjectManager.Target.GetDistance < _pullRange && !_pullFromAfar)
                _pullFromAfar = ToolBox.CheckIfEnemiesOnPull(ObjectManager.Target, _pullRange);

            // Pull from afar
            if (_pullFromAfar && _pullMeleeTimer.ElapsedMilliseconds < 5000 || settings.AlwaysPull
                && ObjectManager.Target.GetDistance <= _pullRange)
            {
                Spell pullMethod = null;

                if (Shoot.IsSpellUsable && Shoot.KnownSpell)
                    pullMethod = Shoot;

                if (Throw.IsSpellUsable && Throw.KnownSpell)
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
            if (_pullMeleeTimer.ElapsedMilliseconds <= 0 && ObjectManager.Target.GetDistance <= _pullRange + 3)
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

            // Stealth
            if (!Me.HaveBuff("Stealth") && !_pullFromAfar && ObjectManager.Target.GetDistance > 15f
                && ObjectManager.Target.GetDistance < 25f && settings.StealthApproach && Backstab.KnownSpell
                && (!ToolBox.HasPoisonDebuff() || settings.StealthWhenPoisoned))
                if (Cast(Stealth))
                    return;

            // Un-Stealth
            if (Me.HaveBuff("Stealth") && _pullFromAfar && ObjectManager.Target.GetDistance > 15f)
                if (Cast(Stealth))
                    return;

            // Stealth approach
            if (Me.HaveBuff("Stealth") && ObjectManager.Target.GetDistance > 3f && !_isStealthApproching && !_pullFromAfar)
            {
                RangeManager.SetRangeToMelee();
                _stealthApproachTimer.Start();
                _isStealthApproching = true;
                if (ObjectManager.Me.IsAlive && ObjectManager.Target.IsAlive)
                {
                    while (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    && ObjectManager.Target.GetDistance > RangeManager.GetRange() - 1
                    && !ToolBox.CheckIfEnemiesOnPull(ObjectManager.Target, _pullRange)
                    && Fight.InFight
                    && _stealthApproachTimer.ElapsedMilliseconds <= 15000
                    && Me.HaveBuff("Stealth"))
                    {
                        // deactivate autoattack
                        ToggleAutoAttack(false);

                        Vector3 position = ToolBox.BackofVector3(ObjectManager.Target.Position, ObjectManager.Target, 2.5f);
                        MovementManager.MoveTo(position);
                        // Wait follow path
                        Thread.Sleep(50);
                    }

                    if (ToolBox.CheckIfEnemiesOnPull(ObjectManager.Target, _pullRange) && Me.HaveBuff("Stealth"))
                    {
                        _pullFromAfar = true;
                        if (Cast(Stealth))
                            return;
                    }

                    // Opener
                    if (ToolBox.MeBehindTarget())
                    {
                        if (settings.UseGarrote)
                            if (Cast(Garrote))
                                MovementManager.StopMove();
                        if (Cast(Backstab))
                            MovementManager.StopMove();
                        if (Cast(CheapShot))
                            MovementManager.StopMove();
                        if (Cast(Hemorrhage) || Cast(SinisterStrike))
                            MovementManager.StopMove();
                    }
                    else
                    {
                        if (CheapShot.KnownSpell)
                        {
                            if (Cast(CheapShot))
                                MovementManager.StopMove();
                        }
                        else if (HaveDaggerInMH() && Gouge.KnownSpell)
                        {
                            if (Cast(Gouge))
                                MovementManager.StopMove();
                        }
                        else
                        {
                            if (Cast(Hemorrhage) || Cast(SinisterStrike))
                                MovementManager.StopMove();
                        }
                    }

                    if (_stealthApproachTimer.ElapsedMilliseconds > 15000)
                    {
                        Logger.Log("_stealthApproachTimer time out");
                        _pullFromAfar = true;
                    }

                    ToggleAutoAttack(true);
                    _isStealthApproching = false;
                }
            }

            // Auto
            if (ObjectManager.Target.GetDistance < 6f && !Me.HaveBuff("Stealth"))
                ToggleAutoAttack(true);
        }

        protected virtual void CombatRotation()
        {
            bool _shouldBeInterrupted = ToolBox.EnemyCasting();
            bool _inMeleeRange = ObjectManager.Target.GetDistance < 6f;
            WoWUnit Target = ObjectManager.Target;

            // Check Auto-Attacking
            ToggleAutoAttack(true);

            // Check if interruptable enemy is in list
            if (_shouldBeInterrupted)
            {
                _fightingACaster = true;
                if (!_casterEnemies.Contains(ObjectManager.Target.Name))
                    _casterEnemies.Add(ObjectManager.Target.Name);
            }

            // Melee ?
            if (_pullMeleeTimer.ElapsedMilliseconds > 0)
                _pullMeleeTimer.Reset();

            if (_meleeTimer.ElapsedMilliseconds <= 0 && _pullFromAfar)
                _meleeTimer.Start();

            if ((_shouldBeInterrupted || _meleeTimer.ElapsedMilliseconds > 5000)
                && !RangeManager.CurrentRangeIsMelee())
            {
                Logger.LogDebug("Going in Melee range 2");
                RangeManager.SetRangeToMelee();
                _meleeTimer.Stop();
            }

            // Kick interrupt
            if (_shouldBeInterrupted)
            {
                Thread.Sleep(Main.humanReflexTime);
                if (Cast(Kick) || Cast(Gouge) || Cast(KidneyShot))
                    return;
            }

            // Mana Tap
            if (Target.Mana > 0 && Target.ManaPercentage > 10)
                if (Cast(ManaTap))
                    return;

            // Arcane Torrent
            if (Target.IsCast && Target.GetDistance < 8)
                if (Cast(ArcaneTorrent))
                    return;

            // Escape Artist
            if (Me.Rooted || Me.HaveBuff("Frostnova"))
                if (Cast(EscapeArtist))
                    return;

            // Stoneform
            if (ToolBox.HasPoisonDebuff() || ToolBox.HasDiseaseDebuff() || Me.HaveBuff("Bleed"))
                if (Cast(Stoneform))
                    return;

            // Will of the Forsaken
            if (Me.HaveBuff("Fear") || Me.HaveBuff("Charm") || Me.HaveBuff("Sleep"))
                if (Cast(WillOfTheForsaken))
                    return;

            // Blood Fury
            if (Target.HealthPercent > 70)
                if (Cast(BloodFury))
                    return;

            // Berserking
            if (Target.HealthPercent > 70)
                if (Cast(Berserking))
                    return;

            // Adrenaline Rush
            if ((ObjectManager.GetNumberAttackPlayer() > 1 || Target.IsElite) && !Me.HaveBuff("Adrenaline Rush"))
                if (Cast(AdrenalineRush))
                    return;

            // Blade Flurry
            if (ObjectManager.GetNumberAttackPlayer() > 1 && !Me.HaveBuff("Blade Flurry"))
                if (Cast(BladeFlurry))
                    return;

            // Riposte
            if (Riposte.IsSpellUsable && (Target.CreatureTypeTarget.Equals("Humanoid") || settings.RiposteAll))
                if (Cast(Riposte))
                    return;

            // Bandage
            if (Target.HaveBuff("Blind"))
            {
                MovementManager.StopMoveTo(true, 500);
                ItemsManager.UseItemByNameOrId(_myBestBandage);
                Logger.Log("Using " + _myBestBandage);
                Usefuls.WaitIsCasting();
                return;
            }

            // Blind
            if (Me.HealthPercent < 40 && !ToolBox.HasDebuff("Recently Bandaged") && _myBestBandage != null
                && settings.UseBlindBandage)
                if (Cast(Blind))
                    return;

            // Evasion
            if (ObjectManager.GetNumberAttackPlayer() > 1 || Me.HealthPercent < 30 && !Me.HaveBuff("Evasion") && Target.HealthPercent > 50)
                if (Cast(Evasion))
                    return;

            // Cloak of Shadows
            if (Me.HealthPercent < 30 && !Me.HaveBuff("Cloak of Shadows") && Target.HealthPercent > 50)
                if (Cast(CloakOfShadows))
                    return;

            // Backstab in combat
            if (IsTargetStunned() && ToolBox.GetMHWeaponType().Equals("Daggers"))
                if (Cast(Backstab))
                    return;

            // Slice and Dice
            if (!Me.HaveBuff("Slice and Dice") && Me.ComboPoint > 1 && Target.HealthPercent > 40)
                if (Cast(SliceAndDice))
                    return;

            // Eviscerate logic
            if (Me.ComboPoint > 0 && Target.HealthPercent < 30
                || Me.ComboPoint > 1 && Target.HealthPercent < 45
                || Me.ComboPoint > 2 && Target.HealthPercent < 60
                || Me.ComboPoint > 3 && Target.HealthPercent < 70)
                if (Cast(Eviscerate))
                    return;

            // GhostlyStrike
            if (Me.ComboPoint < 5 && !IsTargetStunned() &&
                (!_fightingACaster || !Kick.KnownSpell ||
                Me.Energy > ToolBox.GetSpellCost("Ghostly Strike") + ToolBox.GetSpellCost("Kick")))
                if (Cast(GhostlyStrike))
                    return;

            // Hemohrrage
            if (Me.ComboPoint < 5 && !IsTargetStunned() &&
                (!_fightingACaster || !Kick.KnownSpell ||
                Me.Energy > ToolBox.GetSpellCost("Hemorrhage") + ToolBox.GetSpellCost("Kick")))
                if (Cast(Hemorrhage))
                    return;

            // Sinister Strike
            if (Me.ComboPoint < 5 && !IsTargetStunned() &&
                (!_fightingACaster || !Kick.KnownSpell ||
                Me.Energy > ToolBox.GetSpellCost("Sinister Strike") + ToolBox.GetSpellCost("Kick")))
                if (Cast(SinisterStrike))
                    return;
        }

        protected Spell Attack = new Spell("Attack");
        protected Spell Shoot = new Spell("Shoot");
        protected Spell Throw = new Spell("Throw");
        protected Spell Eviscerate = new Spell("Eviscerate");
        protected Spell SinisterStrike = new Spell("Sinister Strike");
        protected Spell Stealth = new Spell("Stealth");
        protected Spell Backstab = new Spell("Backstab");
        protected Spell Gouge = new Spell("Gouge");
        protected Spell Evasion = new Spell("Evasion");
        protected Spell Kick = new Spell("Kick");
        protected Spell Garrote = new Spell("Garrote");
        protected Spell SliceAndDice = new Spell("Slice and Dice");
        protected Spell Vanish = new Spell("Vanish");
        protected Spell CheapShot = new Spell("Cheap Shot");
        protected Spell Riposte = new Spell("Riposte");
        protected Spell BladeFlurry = new Spell("Blade Flurry");
        protected Spell AdrenalineRush = new Spell("Adrenaline Rush");
        protected Spell Sprint = new Spell("Sprint");
        protected Spell CloakOfShadows = new Spell("Cloak of Shadows");
        protected Spell Blind = new Spell("Blind");
        protected Spell KidneyShot = new Spell("Kidney Shot");
        protected Spell Hemorrhage = new Spell("Hemorrhage");
        protected Spell GhostlyStrike = new Spell("Ghostly Strike");
        protected Spell Cannibalize = new Spell("Cannibalize");
        protected Spell WillOfTheForsaken = new Spell("Will of the Forsaken");
        protected Spell BloodFury = new Spell("Blood Fury");
        protected Spell Berserking = new Spell("Berserking");
        protected Spell Stoneform = new Spell("Stoneform");
        protected Spell EscapeArtist = new Spell("Escape Artist");
        protected Spell ManaTap = new Spell("Mana Tap");
        protected Spell ArcaneTorrent = new Spell("Arcane Torrent");

        protected bool Cast(Spell s)
        {
            if (!s.KnownSpell)
                return false;

            CombatDebug("In cast for " + s.Name);
            if (!s.IsSpellUsable || Me.IsCast)
                return false;

            s.Launch();
            return true;
        }

        protected void CombatDebug(string s)
        {
            if (settings.ActivateCombatDebug)
                Logger.CombatDebug(s);
        }

        protected List<string> Bandages()
        {
            return new List<string>
            {
                "Linen Bandage",
                "Heavy Linen Bandage",
                "Wool Bandage",
                "Heavy Wool Bandage",
                "Silk Bandage",
                "Heavy Silk Bandage",
                "Mageweave Bandage",
                "Heavy Mageweave Bandage",
                "Runecloth Bandage",
                "Heavy Runecloth Bandage",
                "Netherweave Bandage",
                "Heavy Netherweave Bandage"
            };
        }

        protected void ToggleAutoAttack(bool activate)
        {
            bool _autoAttacking = Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Attack') " +
                "then isAutoRepeat = true end", "isAutoRepeat");

            if (!_autoAttacking && activate && !ObjectManager.Target.HaveBuff("Gouge")
                && (!ObjectManager.Target.HaveBuff("Blind") || ToolBox.HasDebuff("Recently Bandaged")))
            {
                Logger.Log("Turning auto attack ON");
                ToolBox.CheckAutoAttack(Attack);
            }

            if (!activate && _autoAttacking)
            {
                Logger.Log("Turning auto attack OFF");
                Attack.Launch();
            }
        }

        protected bool IsTargetStunned()
        {
            return ObjectManager.Target.HaveBuff("Gouge") || ObjectManager.Target.HaveBuff("Cheap Shot");
        }

        protected bool HaveDaggerInMH()
        {
            return ToolBox.GetMHWeaponType().Equals("Daggers");
        }

        protected void PoisonWeapon()
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

            if (!hasMainHandEnchant)
            {
                IEnumerable<uint> DP = DeadlyPoisonDictionary
                    .Where(i => i.Key <= Me.Level && ItemsManager.HasItemById(i.Value))
                    .OrderByDescending(i => i.Key)
                    .Select(i => i.Value);

                IEnumerable<uint> IP = InstantPoisonDictionary
                    .Where(i => i.Key <= Me.Level && ItemsManager.HasItemById(i.Value))
                    .OrderByDescending(i => i.Key)
                    .Select(i => i.Value);

                if (DP.Any() || IP.Any())
                {
                    MovementManager.StopMoveTo(true, 1000);
                    MHPoison = DP.Any() ? DP.First() : IP.First();
                    ItemsManager.UseItem(MHPoison);
                    Thread.Sleep(10);
                    Lua.RunMacroText("/use 16");
                    Usefuls.WaitIsCasting();
                    return;
                }
            }
            if (!hasOffHandEnchant && hasoffHandWeapon)
            {

                IEnumerable<uint> IP = InstantPoisonDictionary
                    .Where(i => i.Key <= Me.Level && ItemsManager.HasItemById(i.Value))
                    .OrderByDescending(i => i.Key)
                    .Select(i => i.Value);

                if (IP.Any())
                {
                    MovementManager.StopMoveTo(true, 1000);
                    OHPoison = IP.First();
                    ItemsManager.UseItem(OHPoison);
                    Thread.Sleep(10);
                    Lua.RunMacroText("/use 17");
                    Usefuls.WaitIsCasting();
                    return;
                }
            }
        }

        protected Dictionary<int, uint> InstantPoisonDictionary = new Dictionary<int, uint>
    {
        { 20, 6947 },
        { 28, 6949 },
        { 36, 6950 },
        { 44, 8926 },
        { 52, 8927 },
        { 60, 8928 },
        { 68, 21927 },
        { 73, 43230 },
        { 79, 43231 },
    };

        protected Dictionary<int, uint> DeadlyPoisonDictionary = new Dictionary<int, uint>
    {
        { 30, 2892 },
        { 38, 2893 },
        { 46, 8984 },
        { 54, 8985 },
        { 60, 20844 },
        { 62, 22053 },
        { 70, 22054 },
        { 76, 43232 },
        { 80, 43233 },
    };
    }
}