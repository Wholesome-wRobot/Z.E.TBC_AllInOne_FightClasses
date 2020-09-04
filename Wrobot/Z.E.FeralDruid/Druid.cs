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
using System.ComponentModel;

public static class Druid
{
    private static float _pullRange = 28f;
    private static bool _isStealthApproching;
    internal static Stopwatch _pullMeleeTimer = new Stopwatch();
    internal static Stopwatch _meleeTimer = new Stopwatch();
    internal static Stopwatch _stealthApproachTimer = new Stopwatch();
    internal static Vector3 _fireTotemPosition = null;
    private static WoWLocalPlayer Me = ObjectManager.Me;
    internal static ZEDruidSettings _settings;
    private static bool _fightingACaster = false;
    private static List<string> _casterEnemies = new List<string>();
    private static bool _pullFromAfar = false;
    private static int _bigHealComboCost;
    private static int _smallHealComboCost;

    public static void Initialize()
    {
        Main.Log("Initialized");
        Main.SetRange(_pullRange);

        ZEDruidSettings.Load();
        _settings = ZEDruidSettings.CurrentSetting;

        Talents.InitTalents(_settings.AssignTalents, _settings.UseDefaultTalents, _settings.TalentCodes);

        // Fight end
        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _fightingACaster = false;
            _meleeTimer.Reset();
            _pullMeleeTimer.Reset();
            _stealthApproachTimer.Reset();
            _pullFromAfar = false;
            Main.SetRange(_pullRange);
            _isStealthApproching = false;
        };

        // Fight start
        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
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
        FightEvents.OnFightLoop += (WoWUnit unit, CancelEventArgs cancelable) =>
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
        MovementEvents.OnMoveToPulse += (Vector3 point, CancelEventArgs cancelable) =>
        {
            if (_isStealthApproching && 
            !point.ToString().Equals(ToolBox.BackofVector3(ObjectManager.Target.Position, ObjectManager.Target, 2.5f).ToString()))
                cancelable.Cancel = true;
        };

        // BL Hook
        OthersEvents.OnAddBlackListGuid += (ulong guid, int timeInMilisec, bool isSessionBlacklist, CancelEventArgs cancelable) =>
        {
            Main.LogDebug("BL : " + guid + " ms : " + timeInMilisec + " is session: " + isSessionBlacklist);
            if (Me.HaveBuff("Prowl"))
                cancelable.Cancel = true;
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
                    // Buff rotation
                    if (!Fight.InFight 
                        && ObjectManager.GetNumberAttackPlayer() < 1)
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
                && _settings.UseTravelForm 
                && Me.ManaPercentage > 50
                && Me.ManaPercentage > wManager.wManagerSetting.CurrentSetting.DrinkPercent 
                && !ObjectManager.Target.IsFlightMaster)
                if (Cast(TravelForm))
                    return;

            // Cat Form
            if (!Me.HaveBuff("Cat Form")
                && !Me.HaveBuff("Aquatic Form")
                && (!_settings.UseTravelForm || !TravelForm.KnownSpell || Me.ManaPercentage < 50) 
                && Me.ManaPercentage > wManager.wManagerSetting.CurrentSetting.DrinkPercent
                && !ObjectManager.Target.IsFlightMaster 
                && _settings.CatFormOOC)
            {
                if (Cast(CatForm))
                    return;
            }
        }
    }

    internal static void Pull()
    {
        if (!BearForm.KnownSpell 
            && !CatForm.KnownSpell)
            _pullFromAfar = true;

        // Check if surrounding enemies
        if (ObjectManager.Target.GetDistance < _pullRange 
            && !_pullFromAfar)
            _pullFromAfar = ToolBox.CheckIfEnemiesOnPull(ObjectManager.Target, 20f);

        // Pull from afar
        if (((_pullFromAfar && _pullMeleeTimer.ElapsedMilliseconds < 5000) || _settings.AlwaysPull)
            && ObjectManager.Target.GetDistance <= _pullRange)
            if (PullSpell())
                return;

        // Melee ?
        if (_pullMeleeTimer.ElapsedMilliseconds <= 0 
            && ObjectManager.Target.GetDistance <= _pullRange)
            _pullMeleeTimer.Start();

        if (_pullMeleeTimer.ElapsedMilliseconds > 5000)
        {
            Main.Log("Going in Melee range (pull)");
            Main.SetRangeToMelee();
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
            && _settings.StealthEngage)
            if (Cast(Prowl))
                return;

        // Pull Bear/Cat
        if (Me.HaveBuff("Bear Form") 
            || Me.HaveBuff("Dire Bear Form") 
            || Me.HaveBuff("Cat Form") 
            || !_pullFromAfar)
        {
            Main.SetRangeToMelee();

            // Prowl approach
            if (Me.HaveBuff("Prowl") && ObjectManager.Target.GetDistance > 3f && !_isStealthApproching)
            {
                _stealthApproachTimer.Start();
                _isStealthApproching = true;
                if (ObjectManager.Me.IsAlive && ObjectManager.Target.IsAlive)
                {

                    while (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    && (ObjectManager.Target.GetDistance > 4f || !Claw.IsSpellUsable) 
                    && !ToolBox.CheckIfEnemiesOnPull(ObjectManager.Target, _pullRange) && Fight.InFight
                    && _stealthApproachTimer.ElapsedMilliseconds <= 7000 && Me.HaveBuff("Prowl"))
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

    internal static void CombatRotation()
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
            && !Main.CurrentRangeIsMelee())
        {
            Main.Log("Going in Melee range (combat)");
            Main.SetRangeToMelee();
            _meleeTimer.Stop();
        }

        // Innervate
        if (_settings.UseInnervate 
            && Me.ManaPercentage < 20)
            if (Cast(Innervate))
                return;
        
        // Barkskin + Regrowth + Rejuvenation
        if (_settings.UseBarkskin 
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
            && ObjectManager.GetNumberAttackPlayer() < 2)
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
            else if (ObjectManager.GetNumberAttackPlayer() >= _settings.NumberOfAttackersBearForm
                    && _settings.BearFormOnMultiAggro
                    && _settings.NumberOfAttackersBearForm > 1)
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
            Main.SetRangeToMelee();

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
                && _settings.UseTigersFury 
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
            Main.SetRangeToMelee();

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
                Thread.Sleep(Main._humanReflexTime);
                if (Cast(Bash))
                    return;
            }

            // Enrage
            if (_settings.UseEnrage)
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

    public static void ShowConfiguration()
    {
        ZEDruidSettings.Load();
        ZEDruidSettings.CurrentSetting.ToForm();
        ZEDruidSettings.CurrentSetting.Save();
    }

    private static Spell Attack = new Spell("Attack");
    private static Spell HealingTouch = new Spell("Healing Touch");
    private static Spell Wrath = new Spell("Wrath");
    private static Spell MarkOfTheWild = new Spell("Mark of the Wild");
    private static Spell Moonfire = new Spell("Moonfire");
    private static Spell Rejuvenation = new Spell("Rejuvenation");
    private static Spell Thorns = new Spell("Thorns");
    private static Spell BearForm = new Spell("Bear Form");
    private static Spell DireBearForm = new Spell("Dire Bear Form");
    private static Spell CatForm = new Spell("Cat Form");
    private static Spell TravelForm = new Spell("Travel Form");
    private static Spell Maul = new Spell("Maul");
    private static Spell DemoralizingRoar = new Spell("Demoralizing Roar");
    private static Spell Enrage = new Spell("Enrage");
    private static Spell Regrowth = new Spell("Regrowth");
    private static Spell Bash = new Spell("Bash");
    private static Spell Swipe = new Spell("Swipe");
    private static Spell FaerieFire = new Spell("Faerie Fire");
    private static Spell FaerieFireFeral = new Spell("Faerie Fire (Feral)");
    private static Spell Claw = new Spell("Claw");
    private static Spell Prowl = new Spell("Prowl");
    private static Spell Rip = new Spell("Rip");
    private static Spell Shred = new Spell("Shred");
    private static Spell RemoveCurse = new Spell("Remove Curse");
    private static Spell Rake = new Spell("Rake");
    private static Spell TigersFury = new Spell("Tiger's Fury");
    private static Spell AbolishPoison = new Spell("Abolish Poison");
    private static Spell Ravage = new Spell("Ravage");
    private static Spell FerociousBite = new Spell("Ferocious Bite");
    private static Spell Pounce = new Spell("Pounce");
    private static Spell FrenziedRegeneration = new Spell("Frenzied Regeneration");
    private static Spell Innervate = new Spell("Innervate");
    private static Spell Barkskin = new Spell("Barkskin");
    private static Spell MangleCat = new Spell("Mangle (Cat)");
    private static Spell MangleBear = new Spell("Mangle (Bear)");
    private static Spell Maim = new Spell("Maim");
    private static Spell OmenOfClarity = new Spell("Omen of Clarity");
    private static Spell WarStomp = new Spell("War Stomp");
    private static Spell AquaticForm = new Spell("Aquatic Form");

    private static bool MaulOn()
    {
        return Lua.LuaDoString<bool>("maulon = false; if IsCurrentSpell('Maul') then maulon = true end", "maulon");
    }

    private static bool PullSpell()
    {
        Main.SetRange(_pullRange);
        MovementManager.StopMoveTo(false, 500);
        if ((Me.HaveBuff("Cat Form") 
            || Me.HaveBuff("Bear Form") 
            || Me.HaveBuff("Dire Bear Form")) 
            && FaerieFireFeral.KnownSpell)
        {
            Lua.RunMacroText("/cast Faerie Fire (Feral)()");
            Thread.Sleep(2000);
            return true;
        }
        else if (CatForm.KnownSpell
            && !Me.HaveBuff("Cat Form")
            && FaerieFireFeral.KnownSpell)
        {
            Cast(CatForm);
            return true;
        }
        else
        {
            MovementManager.StopMove();
            Lua.RunMacroText("/cast Moonfire(Rank 1)");
            Thread.Sleep(3000);
        }
        return false;
    }

    private static bool Cast(Spell s)
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
        if (ObjectManager.Target.IsAlive || (!Fight.InFight && ObjectManager.Target.Guid < 1))
            s.Launch();
        return true;
    }

    private static void CombatDebug(string s)
    {
        if (_settings.ActivateCombatDebug)
            Main.CombatDebug(s);
    }
}
