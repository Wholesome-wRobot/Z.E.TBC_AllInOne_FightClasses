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
using WholesomeTBCAIO.Settings;
using WholesomeTBCAIO.Helpers;

namespace WholesomeTBCAIO.Rotations.Druid
{
    public class Druid : IClassRotation
    {
        protected Stopwatch _pullMeleeTimer = new Stopwatch();
        protected Stopwatch _meleeTimer = new Stopwatch();
        protected Stopwatch _stealthApproachTimer = new Stopwatch();
        protected WoWLocalPlayer Me = ObjectManager.Me;
        protected DruidSettings settings;

        protected bool _fightingACaster = false;
        protected List<string> _casterEnemies = new List<string>();
        protected bool _pullFromAfar = false;
        protected int _bigHealComboCost;
        protected int _smallHealComboCost;
        protected float _pullRange = 27f;
        protected bool _isStealthApproching;

        protected Druid specialization;

        public void Initialize(IClassRotation specialization)
        {
            Logger.Log("Initialized");
            RangeManager.SetRange(_pullRange);
            settings = DruidSettings.Current;

            this.specialization = specialization as Druid;
            Talents.InitTalents(settings);

            // Fight end
            FightEvents.OnFightEnd += (guid) =>
            {
                _fightingACaster = false;
                _meleeTimer.Reset();
                _pullMeleeTimer.Reset();
                _stealthApproachTimer.Reset();
                _pullFromAfar = false;
                RangeManager.SetRange(_pullRange);
                _isStealthApproching = false;
            };

            // Fight start
            FightEvents.OnFightStart += (unit, cancelable) =>
            {
                if (Regrowth.KnownSpell)
                {
                    string bearFormSpell = DireBearForm.KnownSpell ? "Dire Bear Form" : "Bear Form";
                    _bigHealComboCost = ToolBox.GetSpellCost("Regrowth") + ToolBox.GetSpellCost("Rejuvenation") +
                    ToolBox.GetSpellCost(bearFormSpell);
                    _smallHealComboCost = ToolBox.GetSpellCost("Regrowth") + ToolBox.GetSpellCost(bearFormSpell);
                }
            };

            // Fight Loop
            FightEvents.OnFightLoop += (unit, cancelable) =>
            {
                if ((ObjectManager.Target.HaveBuff("Pounce") || ObjectManager.Target.HaveBuff("Maim"))
                && !MovementManager.InMovement && Me.IsAlive && !Me.IsCast)
                {
                    if (Me.IsAlive && ObjectManager.Target.IsAlive)
                    {
                        Vector3 position = ToolBox.BackofVector3(ObjectManager.Target.Position, ObjectManager.Target, 2.5f);
                        MovementManager.Go(PathFinder.FindPath(position), false);

                        while (MovementManager.InMovement && Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                        && (ObjectManager.Target.HaveBuff("Pounce") || ObjectManager.Target.HaveBuff("Maim")))
                        {
                            // Wait follow path
                            Thread.Sleep(500);
                        }
                    }
                }
            };

            // We override movement to target when approaching in prowl
            MovementEvents.OnMoveToPulse += (point, cancelable) =>
            {
                if (_isStealthApproching &&
                !point.ToString().Equals(ToolBox.BackofVector3(ObjectManager.Target.Position, ObjectManager.Target, 2.5f).ToString()))
                    cancelable.Cancel = true;
            };

            // BL Hook
            OthersEvents.OnAddBlackListGuid += (guid, timeInMilisec, isSessionBlacklist, cancelable) =>
            {
                Logger.LogDebug("BL : " + guid + " ms : " + timeInMilisec + " is session: " + isSessionBlacklist);
                if (Me.HaveBuff("Prowl"))
                    cancelable.Cancel = true;
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
                            specialization.BuffRotation();

                        // Pull & Combat rotation
                        if (Fight.InFight
                            && ObjectManager.Me.Target > 0UL
                            && ObjectManager.Target.IsAttackable
                            && ObjectManager.Target.IsAlive)
                        {
                            if (!ObjectManager.Me.InCombatFlagOnly)
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
                // Regrowth
                if (Me.HealthPercent < 70
                    && !Me.HaveBuff("Regrowth"))
                    if (Cast(Regrowth))
                        return;

                // Rejuvenation
                if (Me.HealthPercent < 50
                    && !Me.HaveBuff("Rejuvenation")
                    && !Regrowth.KnownSpell)
                    if (Cast(Rejuvenation))
                        return;

                // Healing Touch
                if (Me.HealthPercent < 40
                    && !Regrowth.KnownSpell)
                    if (Cast(HealingTouch))
                        return;

                // Remove Curse
                if (ToolBox.HasCurseDebuff()
                    && RemoveCurse.KnownSpell
                    && RemoveCurse.IsSpellUsable)
                {
                    Lua.RunMacroText("/target player");
                    Thread.Sleep(200);
                    if (Cast(RemoveCurse))
                    {
                        Lua.RunMacroText("/cleartarget");
                        return;
                    }
                }

                // Abolish Poison
                if (ToolBox.HasPoisonDebuff()
                    && RemoveCurse.KnownSpell
                    && RemoveCurse.IsSpellUsable)
                {
                    Lua.RunMacroText("/target player");
                    Thread.Sleep(200);
                    if (Cast(AbolishPoison))
                    {
                        Lua.RunMacroText("/cleartarget");
                        return;
                    }
                }

                // Mark of the Wild
                if (!Me.HaveBuff("Mark of the Wild")
                    && MarkOfTheWild.KnownSpell
                    && MarkOfTheWild.IsSpellUsable)
                {
                    Lua.RunMacroText("/target player");
                    Thread.Sleep(200);
                    if (Cast(MarkOfTheWild))
                        Lua.RunMacroText("/cleartarget");
                    return;
                }

                // Thorns
                if (!Me.HaveBuff("Thorns")
                    && Thorns.KnownSpell
                    && Thorns.IsSpellUsable)
                {
                    Lua.RunMacroText("/target player");
                    Thread.Sleep(200);
                    if (Cast(Thorns))
                        Lua.RunMacroText("/cleartarget");
                    return;
                }

                // Omen of Clarity
                if (!Me.HaveBuff("Omen of Clarity") && OmenOfClarity.IsSpellUsable)
                    if (Cast(OmenOfClarity))
                        return;

                // Aquatic form
                if (Me.IsSwimming
                    && !Me.HaveBuff("Aquatic Form")
                    && Me.ManaPercentage > 50)
                    if (Cast(AquaticForm))
                        return;

                // Travel Form
                if (!Me.HaveBuff("Travel Form")
                    && !Me.HaveBuff("Aquatic Form")
                    && settings.UseTravelForm
                    && Me.ManaPercentage > 50
                    && Me.ManaPercentage > wManager.wManagerSetting.CurrentSetting.DrinkPercent
                    && !ObjectManager.Target.IsFlightMaster)
                    if (Cast(TravelForm))
                        return;

                // Cat Form
                if (!Me.HaveBuff("Cat Form")
                    && !Me.HaveBuff("Aquatic Form")
                    && (!settings.UseTravelForm || !TravelForm.KnownSpell || Me.ManaPercentage < 50)
                    && Me.ManaPercentage > wManager.wManagerSetting.CurrentSetting.DrinkPercent
                    && !ObjectManager.Target.IsFlightMaster
                    && settings.CatFormOOC)
                {
                    if (Cast(CatForm))
                        return;
                }
            }
        }

        protected virtual void Pull()
        {
            if (!BearForm.KnownSpell
                && !CatForm.KnownSpell)
                _pullFromAfar = true;

            // Check if surrounding enemies
            if (!_pullFromAfar)
                _pullFromAfar = ToolBox.CheckIfEnemiesOnPull(ObjectManager.Target, 20f);

            // Get in pull distance
            if (_pullFromAfar && ObjectManager.Target.GetDistance >= _pullRange)
                return;

            // Pull from afar
            if ((_pullFromAfar && _pullMeleeTimer.ElapsedMilliseconds < 5000 || settings.AlwaysPull)
                && ObjectManager.Target.GetDistance <= _pullRange)
                if (PullSpell())
                    return;

            //Main.Log("pullfromafar is " + _pullFromAfar + " timer : " + _pullMeleeTimer.ElapsedMilliseconds);

            // Melee ?
            if (_pullMeleeTimer.ElapsedMilliseconds <= 0
                && ObjectManager.Target.GetDistance <= _pullRange)
                _pullMeleeTimer.Start();

            if (_pullMeleeTimer.ElapsedMilliseconds > 5000)
            {
                Logger.Log("Going in Melee range (pull)");
                RangeManager.SetRangeToMelee();
                ToolBox.CheckAutoAttack(Attack);
                _pullMeleeTimer.Reset();
            }

            // Check if caster in list
            if (_casterEnemies.Contains(ObjectManager.Target.Name))
                _fightingACaster = true;

            // Cat Form
            if (!Me.HaveBuff("Cat Form") && !_pullFromAfar)
                if (Cast(CatForm))
                    return;

            // Prowl
            if (Me.HaveBuff("Cat Form")
                && !_pullFromAfar
                && ObjectManager.Target.GetDistance > 15f
                && ObjectManager.Target.GetDistance < 25f
                && settings.StealthEngage)
                if (Cast(Prowl))
                    return;

            // Pull Bear/Cat
            if (Me.HaveBuff("Bear Form")
                || Me.HaveBuff("Dire Bear Form")
                || Me.HaveBuff("Cat Form")
                || !_pullFromAfar)
            {
                RangeManager.SetRangeToMelee();

                // Prowl approach
                if (Me.HaveBuff("Prowl")
                    && ObjectManager.Target.GetDistance > 3f
                    && !_isStealthApproching)
                {
                    _stealthApproachTimer.Start();
                    _isStealthApproching = true;
                    if (ObjectManager.Me.IsAlive
                        && ObjectManager.Target.IsAlive)
                    {

                        while (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                        && (ObjectManager.Target.GetDistance > 4f || !Claw.IsSpellUsable)
                        && !ToolBox.CheckIfEnemiesOnPull(ObjectManager.Target, 20f)
                        && Fight.InFight
                        && _stealthApproachTimer.ElapsedMilliseconds <= 7000
                        && Me.HaveBuff("Prowl"))
                        {
                            Vector3 position = ToolBox.BackofVector3(ObjectManager.Target.Position, ObjectManager.Target, 2.5f);
                            MovementManager.MoveTo(position);
                            // Wait follow path
                            Thread.Sleep(50);
                        }

                        if (Me.Energy > 80)
                            if (Cast(Pounce))
                                MovementManager.StopMove();

                        if (!Pounce.KnownSpell || Me.Energy <= 80 || !Me.HaveBuff("Prowl"))
                        {
                            Cast(Ravage);
                            if (Cast(Shred) || Cast(Rake) || Cast(Claw))
                                MovementManager.StopMove();
                        }

                        if (_stealthApproachTimer.ElapsedMilliseconds > 7000)
                            _pullFromAfar = true;

                        ToolBox.CheckAutoAttack(Attack);
                        _isStealthApproching = false;
                    }
                }
                return;
            }

            // Pull from distance
            if (_pullFromAfar
                && ObjectManager.Target.GetDistance <= _pullRange)
                if (PullSpell())
                    return;
        }

        protected virtual void CombatRotation()
        {
            bool _shouldBeInterrupted = ToolBox.EnemyCasting();
            bool _inMeleeRange = ObjectManager.Target.GetDistance < 6f;
            WoWUnit Target = ObjectManager.Target;

            // Check Auto-Attacking
            ToolBox.CheckAutoAttack(Attack);

            // Check if fighting a caster
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

            if ((_shouldBeInterrupted || _meleeTimer.ElapsedMilliseconds > 3000)
                && !RangeManager.CurrentRangeIsMelee())
            {
                Logger.Log("Going in Melee range (combat)");
                RangeManager.SetRangeToMelee();
                _meleeTimer.Stop();
            }

            // Innervate
            if (settings.UseInnervate
                && Me.ManaPercentage < 20)
                if (Cast(Innervate))
                    return;

            // Barkskin + Regrowth + Rejuvenation
            if (settings.UseBarkskin
                && Barkskin.KnownSpell
                && Me.HealthPercent < 50
                && !Me.HaveBuff("Regrowth")
                && Me.Mana > _bigHealComboCost + ToolBox.GetSpellCost("Barkskin")
                && (Target.HealthPercent > 15 || Me.HealthPercent < 25))
                if (Cast(Barkskin) && Cast(Regrowth) && Cast(Rejuvenation))
                    return;

            // Regrowth + Rejuvenation
            if (Me.HealthPercent < 50
                && !Me.HaveBuff("Regrowth")
                && Me.Mana > _bigHealComboCost
                && (Target.HealthPercent > 15 || Me.HealthPercent < 25))
                if (Cast(Regrowth) && Cast(Rejuvenation))
                    return;

            // Regrowth
            if (Me.HealthPercent < 50
                && !Me.HaveBuff("Regrowth")
                && Me.Mana > _smallHealComboCost
                && (Target.HealthPercent > 15 || Me.HealthPercent < 25))
                if (Cast(Regrowth))
                    return;

            // Rejuvenation
            if (Me.HealthPercent < 50
                && !Me.HaveBuff("Rejuvenation")
                && !Regrowth.KnownSpell
                && (Target.HealthPercent > 15 || Me.HealthPercent < 25))
                if (Cast(Rejuvenation))
                    return;

            // Healing Touch
            if (Me.HealthPercent < 30
                && !Regrowth.KnownSpell
                && (Target.HealthPercent > 15 || Me.HealthPercent < 25))
                if (Cast(HealingTouch))
                    return;

            // Catorm
            if (!Me.HaveBuff("Cat Form")
                && (ObjectManager.GetNumberAttackPlayer() < settings.NumberOfAttackersBearForm || !BearForm.KnownSpell && !DireBearForm.KnownSpell))
                if (Cast(CatForm))
                    return;

            // Bear Form
            if (!Me.HaveBuff("Bear Form")
                && !Me.HaveBuff("Dire Bear Form"))
            {
                if (!CatForm.KnownSpell)
                {
                    if (Cast(DireBearForm) || Cast(BearForm))
                        return;
                }
                else if (ObjectManager.GetNumberAttackPlayer() >= settings.NumberOfAttackersBearForm
                        && settings.BearFormOnMultiAggro
                        && settings.NumberOfAttackersBearForm > 1)
                {
                    {
                        if (Cast(DireBearForm) || Cast(BearForm))
                            return;
                    }
                }
            }


            #region Cat Form Rotation

            // **************** CAT FORM ROTATION ****************

            if (Me.HaveBuff("Cat Form"))
            {
                RangeManager.SetRangeToMelee();

                // Shred (when behind)
                if (Target.HaveBuff("Pounce"))
                    if (Cast(Shred))
                        return;

                // Faerie Fire
                if (!Target.HaveBuff("Faerie Fire (Feral)")
                    && FaerieFireFeral.KnownSpell
                    && !Target.HaveBuff("Pounce")
                    && ToolBox.CanBleed(Me.TargetObject))
                {
                    Lua.RunMacroText("/cast Faerie Fire (Feral)()");
                    return;
                }

                // Rip
                if (!Target.HaveBuff("Rip")
                    && !Target.HaveBuff("Pounce")
                    && ToolBox.CanBleed(Me.TargetObject))
                {
                    if (Me.ComboPoint >= 3
                        && Target.HealthPercent > 60)
                        if (Cast(Rip))
                            return;

                    if (Me.ComboPoint >= 1
                        && Target.HealthPercent <= 60)
                        if (Cast(Rip))
                            return;
                }

                // Ferocious Bite
                if (FerociousBite.KnownSpell
                    && !Target.HaveBuff("Pounce"))
                {
                    if (Me.ComboPoint >= 3
                        && Target.HealthPercent > 60)
                        if (Cast(FerociousBite))
                            return;

                    if (Me.ComboPoint >= 1
                        && Target.HealthPercent <= 60)
                        if (Cast(FerociousBite))
                            return;
                }

                // Rake
                if (!Target.HaveBuff("Rake")
                    && !Target.HaveBuff("Pounce")
                    && ToolBox.CanBleed(Me.TargetObject))
                    if (Cast(Rake))
                        return;

                // Tiger's Fury
                if (!TigersFury.HaveBuff
                    && settings.UseTigersFury
                    && Me.ComboPoint < 1
                    && !Target.HaveBuff("Pounce")
                    && Me.Energy > 30
                    && TigersFury.IsSpellUsable)
                    TigersFury.Launch();

                // Mangle
                if (Me.ComboPoint < 5
                    && !Target.HaveBuff("Pounce")
                    && MangleCat.KnownSpell)
                {
                    Lua.RunMacroText("/cast Mangle (Cat)()");
                    return;
                }

                // Claw
                if (Me.ComboPoint < 5 && !Target.HaveBuff("Pounce"))
                    if (Cast(Claw))
                        return;
            }

            #endregion

            #region Bear form rotation

            // **************** BEAR FORM ROTATION ****************

            if (Me.HaveBuff("Bear Form") || Me.HaveBuff("Dire Bear Form"))
            {
                RangeManager.SetRangeToMelee();

                // Frenzied Regeneration
                if (Me.HealthPercent < 50)
                    if (Cast(FrenziedRegeneration))
                        return;

                // Faerie Fire
                if (!Target.HaveBuff("Faerie Fire (Feral)")
                    && FaerieFireFeral.KnownSpell
                    && ToolBox.CanBleed(Me.TargetObject))
                {
                    Lua.RunMacroText("/cast Faerie Fire (Feral)()");
                }

                // Swipe
                if (ObjectManager.GetNumberAttackPlayer() > 1 && ToolBox.CheckIfEnemiesClose(8f))
                    if (Cast(Swipe))
                        return;

                // Interrupt with Bash
                if (_shouldBeInterrupted)
                {
                    Thread.Sleep(Main.humanReflexTime);
                    if (Cast(Bash))
                        return;
                }

                // Enrage
                if (settings.UseEnrage)
                    if (Cast(Enrage))
                        return;

                // Demoralizing Roar
                if (!Target.HaveBuff("Demoralizing Roar") && Target.GetDistance < 9f)
                    if (Cast(DemoralizingRoar))
                        return;

                // Maul
                if (!MaulOn() && (!_fightingACaster || Me.Rage > 30))
                    if (Cast(Maul))
                        return;
            }

            #endregion

            #region Human form rotation

            // **************** HUMAN FORM ROTATION ****************

            // Avoid accidental Human Form stay
            if (CatForm.KnownSpell && ToolBox.GetSpellCost("Cat Form") < Me.Mana)
                return;
            if (BearForm.KnownSpell && ToolBox.GetSpellCost("Bear Form") < Me.Mana)
                return;

            if (!Me.HaveBuff("Bear Form")
                && !Me.HaveBuff("Cat Form")
                && !Me.HaveBuff("Dire Bear Form"))
            {
                // Warstomp
                if (ObjectManager.GetNumberAttackPlayer() > 1
                    && Target.GetDistance < 8)
                    if (Cast(WarStomp))
                        return;

                // Moonfire
                if (!Target.HaveBuff("Moonfire")
                    && Me.ManaPercentage > 15
                    && Target.HealthPercent > 15
                    && Me.Level >= 8)
                    if (Cast(Moonfire))
                        return;

                // Wrath
                if (Target.GetDistance <= _pullRange
                    && Me.ManaPercentage > 45
                    && Target.HealthPercent > 30
                    && Me.Level >= 8)
                    if (Cast(Wrath))
                        return;

                // Moonfire Low level DPS
                if (!Target.HaveBuff("Moonfire")
                    && Me.ManaPercentage > 50
                    && Target.HealthPercent > 30
                    && Me.Level < 8)
                    if (Cast(Moonfire))
                        return;

                // Wrath Low level DPS
                if (Target.GetDistance <= _pullRange
                    && Me.ManaPercentage > 60
                    && Target.HealthPercent > 30
                    && Me.Level < 8)
                    if (Cast(Wrath))
                        return;
            }
            #endregion
        }

        protected Spell Attack = new Spell("Attack");
        protected Spell HealingTouch = new Spell("Healing Touch");
        protected Spell Wrath = new Spell("Wrath");
        protected Spell MarkOfTheWild = new Spell("Mark of the Wild");
        protected Spell Moonfire = new Spell("Moonfire");
        protected Spell Rejuvenation = new Spell("Rejuvenation");
        protected Spell Thorns = new Spell("Thorns");
        protected Spell BearForm = new Spell("Bear Form");
        protected Spell DireBearForm = new Spell("Dire Bear Form");
        protected Spell CatForm = new Spell("Cat Form");
        protected Spell TravelForm = new Spell("Travel Form");
        protected Spell Maul = new Spell("Maul");
        protected Spell DemoralizingRoar = new Spell("Demoralizing Roar");
        protected Spell Enrage = new Spell("Enrage");
        protected Spell Regrowth = new Spell("Regrowth");
        protected Spell Bash = new Spell("Bash");
        protected Spell Swipe = new Spell("Swipe");
        protected Spell FaerieFire = new Spell("Faerie Fire");
        protected Spell FaerieFireFeral = new Spell("Faerie Fire (Feral)");
        protected Spell Claw = new Spell("Claw");
        protected Spell Prowl = new Spell("Prowl");
        protected Spell Rip = new Spell("Rip");
        protected Spell Shred = new Spell("Shred");
        protected Spell RemoveCurse = new Spell("Remove Curse");
        protected Spell Rake = new Spell("Rake");
        protected Spell TigersFury = new Spell("Tiger's Fury");
        protected Spell AbolishPoison = new Spell("Abolish Poison");
        protected Spell Ravage = new Spell("Ravage");
        protected Spell FerociousBite = new Spell("Ferocious Bite");
        protected Spell Pounce = new Spell("Pounce");
        protected Spell FrenziedRegeneration = new Spell("Frenzied Regeneration");
        protected Spell Innervate = new Spell("Innervate");
        protected Spell Barkskin = new Spell("Barkskin");
        protected Spell MangleCat = new Spell("Mangle (Cat)");
        protected Spell MangleBear = new Spell("Mangle (Bear)");
        protected Spell Maim = new Spell("Maim");
        protected Spell OmenOfClarity = new Spell("Omen of Clarity");
        protected Spell WarStomp = new Spell("War Stomp");
        protected Spell AquaticForm = new Spell("Aquatic Form");

        protected bool MaulOn()
        {
            return Lua.LuaDoString<bool>("maulon = false; if IsCurrentSpell('Maul') then maulon = true end", "maulon");
        }

        protected bool PullSpell()
        {
            RangeManager.SetRange(_pullRange);
            //MovementManager.StopMoveTo(false, 500);
            if ((Me.HaveBuff("Cat Form")
                || Me.HaveBuff("Bear Form")
                || Me.HaveBuff("Dire Bear Form"))
                && FaerieFireFeral.KnownSpell)
            {
                Logger.Log("Pulling with Faerie Fire (Feral)");
                Lua.RunMacroText("/cast Faerie Fire (Feral)()");
                Thread.Sleep(2000);
                return true;
            }
            else if (CatForm.KnownSpell
                && !Me.HaveBuff("Cat Form")
                && FaerieFireFeral.KnownSpell)
            {
                Logger.Log("Switching to cat form");
                Cast(CatForm);
                return true;
            }
            else if (Moonfire.KnownSpell
                && !ObjectManager.Target.HaveBuff("Moonfire")
                && ObjectManager.Me.Level >= 10)
            {
                Logger.Log("Pulling with Moonfire (Rank 1)");
                Lua.RunMacroText("/cast Moonfire(Rank 1)");
                return true;
            }
            else if (Cast(Wrath))
                return true;

            return false;
        }

        protected bool Cast(Spell s)
        {
            if (!s.KnownSpell)
                return false;

            CombatDebug("*----------- INTO CAST FOR " + s.Name);
            float _spellCD = ToolBox.GetSpellCooldown(s.Name);
            CombatDebug("Cooldown is " + _spellCD);

            if (ToolBox.GetSpellCost(s.Name) > Me.Mana)
            {
                CombatDebug(s.Name + ": Not enough mana, SKIPPING");
                return false;
            }

            if (_spellCD >= 2f)
            {
                CombatDebug("Didn't cast because cd is too long");
                return false;
            }

            if (_spellCD < 2f && _spellCD > 0f)
            {
                if (ToolBox.GetSpellCastTime(s.Name) < 1f)
                {
                    CombatDebug(s.Name + " is instant and low CD, recycle");
                    return true;
                }

                int t = 0;
                while (ToolBox.GetSpellCooldown(s.Name) > 0)
                {
                    Thread.Sleep(50);
                    t += 50;
                    if (t > 2000)
                    {
                        CombatDebug(s.Name + ": waited for tool long, give up");
                        return false;
                    }
                }
                Thread.Sleep(100 + Usefuls.Latency);
                CombatDebug(s.Name + ": waited " + (t + 100) + " for it to be ready");
            }

            if (!s.IsSpellUsable)
            {
                CombatDebug("Didn't cast because spell somehow not usable");
                return false;
            }

            CombatDebug("Launching");
            if (ObjectManager.Target.IsAlive || !Fight.InFight && ObjectManager.Target.Guid < 1)
                s.Launch();
            return true;
        }

        protected void CombatDebug(string s)
        {
            if (settings.ActivateCombatDebug)
                Logger.CombatDebug(s);
        }
    }
}