using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public static class Warlock
{
    private static WoWLocalPlayer Me = ObjectManager.Me;
    private static float _maxRange = 27f;
    private static bool _usingWand = false;
    private static int _innerManaSaveThreshold = 20;
    private static bool _iCanUseWand = ToolBox.HaveRangedWeaponEquipped();
    private static ZEWarlockSettings _settings;
    private static readonly BackgroundWorker _petPulseThread = new BackgroundWorker();
    private static Stopwatch _addCheckTimer = new Stopwatch();
    private static int _saveDrinkPercent = wManager.wManagerSetting.CurrentSetting.DrinkPercent;

    public static void Initialize()
    {
        Main.Log("Initialized");
        ZEWarlockSettings.Load();
        _settings = ZEWarlockSettings.CurrentSetting;
        Talents.InitTalents(_settings.AssignTalents, _settings.UseDefaultTalents, _settings.TalentCodes);

        Main.SetRange(_maxRange);
        _petPulseThread.DoWork += PetThread;
        _petPulseThread.RunWorkerAsync();

        // Set pet mode
        if (_settings.PetInPassiveWhenOOC)
            Lua.LuaDoString("PetPassiveMode();");
        else
            Lua.LuaDoString("PetDefensiveMode();");
        
        // Fight end
        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _usingWand = false;
            _iCanUseWand = false;
            Main.SetRange(_maxRange);
            _addCheckTimer.Reset();
            if (_settings.PetInPassiveWhenOOC)
                Lua.LuaDoString("PetPassiveMode();");
        };

        // Fight start
        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            if (UseWand.IsSpellUsable)
                _iCanUseWand = true;
            Lua.LuaDoString("PetDefensiveMode();");

            // Imp Firebolt
            if (PetAndConsumables.MyWarlockPet().Equals("Imp"))
                ToolBox.TogglePetSpellAuto("Firebolt", true);

            // Imp BloodPact
            if (PetAndConsumables.MyWarlockPet().Equals("Imp"))
                ToolBox.TogglePetSpellAuto("Blood Pact", true);
        };

        Rotation();
    }
    
    public static void Dispose()
    {
        Main.Log("Stop in progress.");
        _petPulseThread.DoWork -= PetThread;
        _petPulseThread.Dispose();
        Lua.LuaDoString("PetPassiveMode();");
        wManager.wManagerSetting.CurrentSetting.DrinkPercent = _saveDrinkPercent;
    }

    // Pet thread
    private static void PetThread(object sender, DoWorkEventArgs args)
    {
        while (Main._isLaunched)
        {
            try
            {
                if (Conditions.InGameAndConnectedAndProductStartedNotInPause && !ObjectManager.Me.IsOnTaxi && ObjectManager.Me.IsAlive
                    && ObjectManager.Pet.IsValid && !Main.HMPrunningAway)
                {
                    // Voidwalker Torment
                    if (PetAndConsumables.MyWarlockPet().Equals("Voidwalker") 
                        && ObjectManager.Target.Target == Me.Guid
                        && Me.InCombatFlagOnly 
                        && !_settings.AutoTorment)
                        ToolBox.PetSpellCast("Torment");
                }
            }
            catch (Exception arg)
            {
                Logging.WriteError(string.Concat(arg), true);
            }
            Thread.Sleep(300);
        }
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
                    if (!Me.InCombatFlagOnly)
                    {
                        BuffRotation();
                    }

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
        if (!Me.IsMounted)
        {
            // Make sure we have mana to summon
            if (!ObjectManager.Pet.IsValid && ObjectManager.Me.ManaPercentage < 95 && !ObjectManager.Me.HaveBuff("Drink") &&
                ((SummonVoidwalker.KnownSpell && !SummonVoidwalker.IsSpellUsable) && ToolBox.CountItemStacks("Soul Shard") > 0 ||
                (SummonImp.KnownSpell && !SummonImp.IsSpellUsable && !SummonVoidwalker.KnownSpell)))
            {
                Main.Log("Not enough mana to summon, forcing regen");
                wManager.wManagerSetting.CurrentSetting.DrinkPercent = 95;
                Thread.Sleep(1000);
                return;
            }
            else
                wManager.wManagerSetting.CurrentSetting.DrinkPercent = _saveDrinkPercent;

            // Switch Auto Torment & Suffering off
            if (PetAndConsumables.MyWarlockPet().Equals("Voidwalker"))
            {
                ToolBox.TogglePetSpellAuto("Torment", _settings.AutoTorment);
                ToolBox.TogglePetSpellAuto("Suffering", false);
            }

            // Summon Felguard
            if ((!ObjectManager.Pet.IsValid 
                || PetAndConsumables.MyWarlockPet().Equals("Voidwalker") || PetAndConsumables.MyWarlockPet().Equals("Imp")) 
                && SummonFelguard.KnownSpell)
            {
                Thread.Sleep(Usefuls.Latency + 500); // Safety for Mount check
                if (!ObjectManager.Me.IsMounted)
                {
                    if (Cast(FelDomination))
                        Thread.Sleep(200);
                    if (Cast(SummonFelguard))
                        return;
                }
            }

            // Summon Felguard for mana or health
            if (SummonFelguard.KnownSpell && (ObjectManager.Pet.ManaPercentage < 20 || ObjectManager.Pet.HealthPercent < 20) 
                && ObjectManager.Pet.IsValid)
            {
                Thread.Sleep(Usefuls.Latency + 500); // Safety for Mount check
                if (!ObjectManager.Me.IsMounted)
                {
                    if (Cast(FelDomination))
                        Thread.Sleep(200);
                    if (Cast(SummonFelguard))
                        return;
                }
            }

            // Summon Void Walker
            if ((!ObjectManager.Pet.IsValid || !PetAndConsumables.MyWarlockPet().Equals("Voidwalker")) && SummonVoidwalker.KnownSpell
                && !SummonFelguard.KnownSpell)
            {
                Thread.Sleep(Usefuls.Latency + 500); // Safety for Mount check
                if (!ObjectManager.Me.IsMounted)
                {
                    if (Cast(FelDomination))
                        Thread.Sleep(200);
                    if (Cast(SummonVoidwalker))
                        return;
                }
            }

            // Summon Void Walker for mana
            if ((PetAndConsumables.MyWarlockPet().Equals("Voidwalker")) && SummonVoidwalker.KnownSpell && ObjectManager.Pet.ManaPercentage < 20
                && !SummonFelguard.KnownSpell)
            {
                Thread.Sleep(Usefuls.Latency + 500); // Safety for Mount check
                if (!ObjectManager.Me.IsMounted)
                {
                    if (Cast(FelDomination))
                        Thread.Sleep(200);
                    if (Cast(SummonVoidwalker))
                        return;
                }
            }

            // Summon Imp
            if (!ObjectManager.Pet.IsValid && SummonImp.KnownSpell && 
                (!SummonVoidwalker.KnownSpell || ToolBox.CountItemStacks("Soul Shard") < 1))
            {
                Thread.Sleep(Usefuls.Latency + 500); // Safety for Mount check
                if (!ObjectManager.Me.IsMounted)
                {
                    if (Cast(FelDomination))
                        Thread.Sleep(200);
                    if (Cast(SummonImp))
                        return;
                }
            }

            // Life Tap
            if (Me.HealthPercent > Me.ManaPercentage && _settings.UseLifeTap)
                if (Cast(LifeTap))
                    return;

            // Unending Breath
            if (!Me.HaveBuff("Unending Breath") && UnendingBreath.KnownSpell && UnendingBreath.IsSpellUsable 
                && _settings.UseUnendingBreath)
            {
                Lua.RunMacroText("/target player");
                if (Cast(UnendingBreath))
                {
                    Lua.RunMacroText("/cleartarget");
                    return;
                }
            }

            // Demon Skin
            if (!Me.HaveBuff("Demon Skin") && !DemonArmor.KnownSpell && DemonSkin.KnownSpell)
                if (Cast(DemonSkin))
                    return;
            
            // Demon Armor
            if ((!Me.HaveBuff("Demon Armor") || Me.HaveBuff("Demon Skin")) && DemonArmor.KnownSpell && 
                (!FelArmor.KnownSpell || (FelArmor.KnownSpell && !_settings.UseFelArmor)))
                if (Cast(DemonArmor))
                    return;

            // Soul Link
            if (SoulLink.KnownSpell && !Me.HaveBuff("Soul Link"))
                if (Cast(SoulLink))
                    return;

            // Fel Armor
            if (!Me.HaveBuff("Fel Armor") && FelArmor.KnownSpell && _settings.UseFelArmor)
                if (Cast(FelArmor))
                    return;

            // Health Funnel
            if (ObjectManager.Pet.HealthPercent < 50 && Me.HealthPercent > 40 && ObjectManager.Pet.GetDistance < 19
                && !ObjectManager.Pet.InCombatFlagOnly && HealthFunnel.KnownSpell)
            {
                Fight.StopFight();
                MovementManager.StopMove();
                if (PetAndConsumables.MyWarlockPet().Equals("Voidwalker") && ToolBox.GetPetSpellIndex("Consume Shadows") != 0)
                {
                    ToolBox.PetSpellCast("Consume Shadows");
                    Usefuls.WaitIsCasting();
                    MovementManager.StopMove();
                    Thread.Sleep(500);
                }


                ToolBox.StopWandWaitGCD(UseWand, ShadowBolt);
                MovementManager.StopMove();
                MovementManager.StopMoveNewThread();
                if (Cast(HealthFunnel))
                {
                    Thread.Sleep(500);
                    Usefuls.WaitIsCasting();
                    return;
                }
            }

            // Health Stone
            if (!PetAndConsumables.HaveHealthstone())
                if (Cast(CreateHealthStone))
                    return;

            // Create Soul Stone
            if (!PetAndConsumables.HaveSoulstone() && CreateSoulstone.KnownSpell)
                if (Cast(CreateSoulstone))
                    return;

            // Use Soul Stone
            if (!Me.HaveBuff("Soulstone Resurrection") && CreateSoulstone.KnownSpell && ToolBox.HaveOneInList(PetAndConsumables.SoulStones())
                && ToolBox.GetItemCooldown(PetAndConsumables.SoulStones()) <= 0)
            {
                MovementManager.StopMove();
                Lua.RunMacroText("/target player");
                PetAndConsumables.UseSoulstone();
                Usefuls.WaitIsCasting();
                Lua.RunMacroText("/cleartarget");
            }

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
        // Pet attack
        if (ObjectManager.Pet.Target != ObjectManager.Me.Target)
            Lua.LuaDoString("PetAttack();", false);

        // Life Tap
        if (Me.HealthPercent > Me.ManaPercentage && _settings.UseLifeTap)
            if (Cast(LifeTap))
                return;

        // Amplify Curse
        if (AmplifyCurse.IsSpellUsable && AmplifyCurse.KnownSpell)
            AmplifyCurse.Launch();

        // Siphon Life
        if (Me.HealthPercent < 90 && _settings.UseSiphonLife && ObjectManager.Target.GetDistance < _maxRange + 2)
            if (Cast(SiphonLife))
                return;

        // Unstable Affliction
        if (ObjectManager.Target.GetDistance < _maxRange + 2)
            if (Cast(UnstableAffliction))
                return;

        // Curse of Agony
        if (ObjectManager.Target.GetDistance < _maxRange + 2)
            if (Cast(CurseOfAgony))
                return;

        // Corruption
        if (ObjectManager.Target.GetDistance < _maxRange + 2)
            if (Cast(Corruption))
                return;

        // Immolate
        if (ObjectManager.Target.GetDistance < _maxRange + 2 && !Corruption.KnownSpell && ToolBox.CanBleed(ObjectManager.Target))
            if (Cast(Immolate))
                return;

        // Shadow Bolt
        if (ObjectManager.Target.GetDistance < _maxRange + 2 && !Immolate.KnownSpell)
            if (Cast(ShadowBolt))
                return;
    }

    internal static void CombatRotation()
    {
        _usingWand = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(5019); " +
            "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");
        WoWUnit Me = ObjectManager.Me;
        WoWUnit Target = ObjectManager.Target;
        double _myManaPC = Me.ManaPercentage;
        bool _overLowManaThreshold = _myManaPC > _innerManaSaveThreshold;

        // Multi aggro
        /*if (ObjectManager.GetNumberAttackPlayer() > 1 && Fear.KnownSpell &&
            (_addCheckTimer.ElapsedMilliseconds > 3000 || _addCheckTimer.ElapsedMilliseconds <= 0))
        {
            _addCheckTimer.Restart();
            WoWUnit _currenTarget = ObjectManager.Target;
            List<WoWUnit> _listUnitsAttackingMe = ObjectManager.GetUnitAttackPlayer();
            foreach (WoWUnit unit in _listUnitsAttackingMe)
            {
                Thread.Sleep(500);
                if (unit.Target == Me.Guid && unit.Guid != Me.Target && PetAndConsumables.MyWarlockPet().Equals("Voidwalker"))
                {
                    ulong saveTarget = Me.Target;
                    if (Cast(SoulShatter))
                    {
                        _addCheckTimer.Reset();
                        Thread.Sleep(500 + Usefuls.Latency);
                        return;
                    }
                    Lua.RunMacroText("/cleartarget");
                    Me.Target = unit.Guid;
                    Thread.Sleep(200 + Usefuls.Latency);
                    if (_settings.FearAdds)
                        if (Cast(Fear))
                        {
                            Thread.Sleep(200 + Usefuls.Latency);
                            Me.Target = saveTarget;
                        }
                }
            }
        }*/

        // Pet attack
        if (ObjectManager.Pet.Target != ObjectManager.Me.Target)
            Lua.LuaDoString("PetAttack();", false);

        // Mana Tap
        if (Target.Mana > 0 && Target.ManaPercentage > 10)
            if (Cast(ManaTap))
                return;

        // Arcane Torrent
        if ((Me.HaveBuff("Mana Tap") && Me.ManaPercentage < 50)
            || (Target.IsCast && Target.GetDistance < 8))
            if (Cast(ArcaneTorrent))
                return;

        // Will of the Forsaken
        if (Me.HaveBuff("Fear") || Me.HaveBuff("Charm") || Me.HaveBuff("Sleep"))
            if (Cast(WillOfTheForsaken))
                return;

        // Escape Artist
        if (Me.Rooted || Me.HaveBuff("Frostnova"))
            if (Cast(EscapeArtist))
                return;

        // Berserking
        if (Target.HealthPercent > 70)
            if (Cast(Berserking))
                return;

        // Drain Soul
        if (ToolBox.CountItemStacks("Soul Shard") < _settings.NumberOfSoulShards && Target.HealthPercent < 40)
            if (Cast(DrainSoul))
                return;

        // Use Health Stone
        if (Me.HealthPercent < 15)
            PetAndConsumables.UseHealthstone();

        // Shadow Trance
        if (Me.HaveBuff("Shadow Trance") && _overLowManaThreshold)
            if (Cast(ShadowBolt))
                return;

        // Siphon Life
        if (Me.HealthPercent < 90 && _overLowManaThreshold && Target.HealthPercent > 20
            && !Target.HaveBuff("Siphon Life") && _settings.UseSiphonLife)
            if (Cast(SiphonLife))
                return;

        // Death Coil
        if (Me.HealthPercent < 20)
            if (Cast(DeathCoil))
                return;

        // Drain Life low
        if (Me.HealthPercent < 30 && Target.HealthPercent > 20)
            if (Cast(DrainLife))
                return;

        // Curse of Agony
        if (ObjectManager.Target.GetDistance < _maxRange && !Target.HaveBuff("Curse of Agony") && _overLowManaThreshold
            && Target.HealthPercent > 20)
            if (Cast(CurseOfAgony))
                return;

        // Unstable Affliction
        if (ObjectManager.Target.GetDistance < _maxRange && !Target.HaveBuff("Unstable Affliction") && _overLowManaThreshold
            && Target.HealthPercent > 30)
            if (Cast(UnstableAffliction))
                return;

        // Corruption
        if (ObjectManager.Target.GetDistance < _maxRange && !Target.HaveBuff("Corruption") && _overLowManaThreshold
            && Target.HealthPercent > 20)
            if (Cast(Corruption))
                return;

        // Immolate
        if (ObjectManager.Target.GetDistance < _maxRange && !Target.HaveBuff("Immolate") && _overLowManaThreshold
            && Target.HealthPercent > 30 && (_settings.UseImmolateHighLevel || !UnstableAffliction.KnownSpell)
            && ToolBox.CanBleed(ObjectManager.Target))
            if (Cast(Immolate))
                return;

        // Drain Life high
        if (Me.HealthPercent < 70 && Target.HealthPercent > 20)
            if (Cast(DrainLife))
                return;

        // Health Funnel
        if (ObjectManager.Pet.IsValid && ObjectManager.Pet.HealthPercent < 30 && Me.HealthPercent > 30)
        {
            Main.SetRange(19f);
            if (HealthFunnel.IsDistanceGood && Cast(HealthFunnel))
                return;
        }

        // Dark Pact
        if (Me.ManaPercentage < 70 && ObjectManager.Pet.Mana > 0 && ObjectManager.Pet.ManaPercentage > 60
            && _settings.UseDarkPact)
            if (Cast(DarkPact))
                return;

        // Drain Mana
        if (Me.ManaPercentage < 70 && Target.Mana > 0 && Target.ManaPercentage > 30)
            if (Cast(DrainMana))
                return;

        // Incinerate
        if (ObjectManager.Target.GetDistance < _maxRange && Target.HaveBuff("Immolate") && _overLowManaThreshold
            && Target.HealthPercent > 30 && (_settings.UseIncinerate))
            if (Cast(Incinerate))
                return;

        // Shadow Bolt
        if ((!_settings.PrioritizeWandingOverSB || !_iCanUseWand) && 
            (ObjectManager.Target.HealthPercent > 50 || (Me.ManaPercentage > 90 && ObjectManager.Target.HealthPercent > 10)) 
            && _myManaPC > 40 && ObjectManager.Target.GetDistance < _maxRange)
            if (Cast(ShadowBolt))
                return;

        // Life Tap
        if (Me.HealthPercent > 50 && Me.ManaPercentage < 40 && !ObjectManager.Target.IsTargetingMe
            && _settings.UseLifeTap)
            if (Cast(LifeTap))
                return;

        // Use Wand
        if (!_usingWand 
            && _iCanUseWand 
            && ObjectManager.Target.GetDistance <= _maxRange + 2)
        {
            Main.SetRange(_maxRange);
            if (Cast(UseWand, false))
                return;
        }

        // Go in melee because nothing else to do
        if (!_usingWand && !UseWand.IsSpellUsable 
            && !Main.CurrentRangeIsMelee()
            && Target.IsAlive)
        {
            Main.Log("Going in melee");
            Main.SetRangeToMelee();
            return;
        }
    }

    public static void ShowConfiguration()
    {
        ZEWarlockSettings.Load();
        ZEWarlockSettings.CurrentSetting.ToForm();
        ZEWarlockSettings.CurrentSetting.Save();
    }

    private static Spell DemonSkin = new Spell("Demon Skin");
    private static Spell DemonArmor = new Spell("Demon Armor");
    private static Spell ShadowBolt = new Spell("Shadow Bolt");
    private static Spell UseWand = new Spell("Shoot");
    private static Spell Fear = new Spell("Fear");
    private static Spell Immolate = new Spell("Immolate");
    private static Spell Corruption = new Spell("Corruption");
    private static Spell LifeTap = new Spell("Life Tap");
    private static Spell SummonImp = new Spell("Summon Imp");
    private static Spell SummonVoidwalker = new Spell("Summon Voidwalker");
    private static Spell SummonFelguard = new Spell("Summon Felguard");
    private static Spell CurseOfAgony = new Spell("Curse of Agony");
    private static Spell DrainSoul = new Spell("Drain Soul");
    private static Spell DrainLife = new Spell("Drain Life");
    private static Spell CreateHealthStone = new Spell("Create HealthStone");
    private static Spell HealthFunnel = new Spell("Health Funnel");
    private static Spell CreateSoulstone = new Spell("Create Soulstone");
    private static Spell AmplifyCurse = new Spell("Amplify Curse");
    private static Spell UnendingBreath = new Spell("Unending Breath");
    private static Spell SiphonLife = new Spell("Siphon Life");
    private static Spell DrainMana = new Spell("Drain Mana");
    private static Spell DarkPact = new Spell("Dark Pact");
    private static Spell UnstableAffliction = new Spell("Unstable Affliction");
    private static Spell DeathCoil = new Spell("Death Coil");
    private static Spell FelArmor = new Spell("Fel Armor");
    private static Spell Incinerate = new Spell("Incinerate");
    private static Spell SoulShatter = new Spell("Soulshatter");
    private static Spell FelDomination = new Spell("Fel Domination");
    private static Spell Cannibalize = new Spell("Cannibalize");
    private static Spell WillOfTheForsaken = new Spell("Will of the Forsaken");
    private static Spell Berserking = new Spell("Berserking");
    private static Spell EscapeArtist = new Spell("Escape Artist");
    private static Spell ManaTap = new Spell("Mana Tap");
    private static Spell ArcaneTorrent = new Spell("Arcane Torrent");
    private static Spell SoulLink = new Spell("Soul Link");

    private static bool Cast(Spell s, bool castEvenIfWanding = true)
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

        if (_usingWand && !castEvenIfWanding)
        {
            CombatDebug("Didn't cast because we were backing up or wanding");
            return false;
        }

        if (_spellCD >= 2f)
        {
            CombatDebug("Didn't cast because cd is too long");
            return false;
        }

        if (_usingWand && castEvenIfWanding)
            ToolBox.StopWandWaitGCD(UseWand, ShadowBolt);

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
