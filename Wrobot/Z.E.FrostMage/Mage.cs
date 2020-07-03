using System;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.Linq;

public static class Mage
{
    private static MageFoodManager _foodManager = new MageFoodManager();
    private static float _meleeRange = 5f;
    private static float _range = 28f;
    private static bool _usingWand = false;
    private static bool _isBackingUp = false;
    private static WoWLocalPlayer Me = ObjectManager.Me;
    private static bool _iCanUseWand = ToolBox.HaveRangedWeaponEquipped();
    private static ZEMageSettings _settings;

    public static void Initialize()
    {
        Main.settingRange = _range;
        Main.Log("Initialized.");
        ZEMageSettings.Load();
        _settings = ZEMageSettings.CurrentSetting;
        Talents.InitTalents(_settings.AssignTalents, _settings.UseDefaultTalents, _settings.TalentCodes);

        // Fight end
        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _isBackingUp = false;
            _iCanUseWand = false;
            _usingWand = false;
            Main.settingRange = _range;
        };

        // Fight start
        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            _iCanUseWand = ToolBox.HaveRangedWeaponEquipped();
        };

        // Fight Loop
        FightEvents.OnFightLoop += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            bool condition = ((ObjectManager.Target.HaveBuff("Frostbite") || ObjectManager.Target.HaveBuff("Frost Nova")) &&
                ObjectManager.Target.IsAlive && ObjectManager.Target.GetDistance < _meleeRange + 4 && !Me.IsCast && !_isBackingUp
                && ObjectManager.Target.HealthPercent > 5 && Main.settingRange != _meleeRange);
            if (condition)
            {
                Main.LogDebug("Backing up");
                _isBackingUp = true;
                var pos = 1;
                if (Me.IsAlive && ObjectManager.Target.IsAlive && pos == 1)
                {
                    Vector3 position = ToolBox.BackofVector3(Me.Position, Me, 15f);
                    MovementManager.Go(PathFinder.FindPath(position), false);

                    while (MovementManager.InMovement && Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    && ObjectManager.Target.GetDistance < 10f && ObjectManager.Target.IsAlive
                    && (ObjectManager.Target.HaveBuff("Frostbite") || ObjectManager.Target.HaveBuff("Frost Nova")))
                    {
                        // Wait follow path
                        Thread.Sleep(200);
                        if (_settings.BlinkWhenBackup)
                            if (Cast(Blink))
                                Main.LogDebug("Blink away");
                        pos = 0;
                    }
                    _isBackingUp = false;
                }
            }
        };

        Rotation();
	}

    public static void Dispose()
    {
        _usingWand = false;
        _isBackingUp = false;
        Main.Log("Stopped in progress.");
	}

    public static void ShowConfiguration()
    {
        ZEMageSettings.Load();
        ZEMageSettings.CurrentSetting.ToForm();
        ZEMageSettings.CurrentSetting.Save();
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
                    if (!Fight.InFight && !ObjectManager.Me.InCombatFlagOnly && !Me.IsMounted)
                        BuffRotation();

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
        _foodManager.CheckIfEnoughFoodAndDrinks();
        _foodManager.CheckIfThrowFoodAndDrinks();
        _foodManager.CheckIfHaveManaStone();

        // Frost Armor
        if (!Me.HaveBuff("Ice Armor"))
            if (Cast(IceArmor))
                return;

        // Frost Armor
        if (!Me.HaveBuff("Frost Armor") && !IceArmor.KnownSpell)
            if (Cast(FrostArmor))
                return;

        // Arcane Intellect
        if (!Me.HaveBuff("Arcane Intellect") && ArcaneIntellect.KnownSpell && ArcaneIntellect.IsSpellUsable)
        {
            Lua.RunMacroText("/target player");
            if (Cast(ArcaneIntellect))
            {
                Lua.RunMacroText("/cleartarget");
                return;
            }
        }

        // Evocation
        if (Me.ManaPercentage < 30)
            if (Cast(Evocation))
                return;

        
        // Cannibalize
        if (ObjectManager.GetObjectWoWUnit().Where(u => u.GetDistance <= 8 && u.IsDead && (u.CreatureTypeTarget == "Humanoid" || u.CreatureTypeTarget == "Undead")).Count() > 0)
        {
            if (Me.HealthPercent < 50 && !Me.HaveBuff("Drink") && !Me.HaveBuff("Food") && Me.IsAlive && Cannibalize.KnownSpell && Cannibalize.IsSpellUsable)
                if (Cast(Cannibalize))
                    return;
        }
    }

    internal static void Pull()
    {
        WoWUnit _target = ObjectManager.Target;

        // Ice Barrier
        if (IceBarrier.IsSpellUsable && !Me.HaveBuff("Ice Barrier"))
            if (Cast(IceBarrier))
                return;

        // Frost Bolt
        if (_target.GetDistance < _range && Me.Level >= 6 && (_target.HealthPercent > _settings.WandThreshold
            || ObjectManager.GetNumberAttackPlayer() > 1 || Me.HealthPercent < 30 || !_iCanUseWand))
            if (Cast(Frostbolt))
                return;

        // Low level Frost Bolt
        if (_target.GetDistance < _range && _target.HealthPercent > 30 && Me.Level < 6)
            if (Cast(Frostbolt))
                return;

        // Low level FireBall
        if (_target.GetDistance < _range && !Frostbolt.KnownSpell && _target.HealthPercent > 30)
            if (Cast(Fireball))
                return;
    }

    internal static void CombatRotation()
    {
        Lua.LuaDoString("PetAttack();", false);
        bool _hasCurse = ToolBox.HasCurseDebuff();
        WoWUnit Target = ObjectManager.Target;
        _usingWand = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(5019); " +
            "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");

        // Remove Curse
        if (_hasCurse)
        {
            Thread.Sleep(Main._humanReflexTime);
            if (Cast(RemoveCurse))
                return;
        }

        // Mana Tap
        if (Target.Mana > 0 && Target.ManaPercentage > 10)
            if (Cast(ManaTap))
                return;

        // Arcane Torrent
        if ((Me.HaveBuff("Mana Tap") && Me.ManaPercentage < 50)
            || (Target.IsCast && Target.GetDistance < 8))
            if (Cast(ArcaneTorrent))
                return;

        // Gift of the Naaru
        if (ObjectManager.GetNumberAttackPlayer() > 1 && Me.HealthPercent < 50)
            if (Cast(GiftOfTheNaaru))
                return;

        // Escape Artist
        if (Me.Rooted || Me.HaveBuff("Frostnova"))
            if (Cast(EscapeArtist))
                return;

        // Will of the Forsaken
        if (Me.HaveBuff("Fear") || Me.HaveBuff("Charm") || Me.HaveBuff("Sleep"))
            if (Cast(WillOfTheForsaken))
                return;

        // Berserking
        if (Target.HealthPercent > 70)
            if (Cast(Berserking))
                return;

        // Summon Water Elemental
        if (Target.HealthPercent > 95 || ObjectManager.GetNumberAttackPlayer() > 1)
            if (Cast(SummonWaterElemental))
                return;

        // Ice Barrier
        if (IceBarrier.IsSpellUsable && !Me.HaveBuff("Ice Barrier"))
            if (Cast(IceBarrier))
                return;

        // Mana Shield
        if (!Me.HaveBuff("Mana Shield") && Me.HealthPercent < 30 && Me.ManaPercentage > 50)
            if (Cast(ManaShield))
                return;

        // Cold Snap
        if (ObjectManager.GetNumberAttackPlayer() > 1 && !Me.HaveBuff("Icy Veins") && !IcyVeins.IsSpellUsable)
            if (Cast(ColdSnap))
                return;

        // Icy Veins
        if ((ObjectManager.GetNumberAttackPlayer() > 1 && _settings.IcyVeinMultiPull)
            || !_settings.IcyVeinMultiPull)
            if (Cast(IcyVeins))
                return;

        // Use Mana Stone
        if (((ObjectManager.GetNumberAttackPlayer() > 1 && Me.ManaPercentage < 50) || Me.ManaPercentage < 5)
            && _foodManager.ManaStone != "")
        {
            _foodManager.UseManaStone();
            _foodManager.ManaStone = "";
        }

        // Ice Lance
        if (Target.HaveBuff("Frostbite") || Target.HaveBuff("Frost Nova"))
            if (Cast(IceLance))
                return;

        // Frost Nova
        if (Target.GetDistance < _meleeRange + 2 && Target.HealthPercent > 10 && !Target.HaveBuff("Frostbite"))
            if (Cast(FrostNova))
                return;

        // Fire Blast
        if (Target.GetDistance < 20f && Target.HealthPercent > 30f)
            if (Cast(FireBlast))
                return;

        // Cone of Cold
        if (Target.GetDistance < 10 && _settings.UseConeOfCold && !_isBackingUp && !MovementManager.InMovement)
            if (Cast(ConeOfCold))
                return;

        // Frost Bolt
        if (Target.GetDistance < _range && Me.Level >= 6 && (Target.HealthPercent > _settings.WandThreshold
            || ObjectManager.GetNumberAttackPlayer() > 1 || Me.HealthPercent < 40 || !_iCanUseWand))
            if (Cast(Frostbolt, true))
                return;

        // Low level Frost Bolt
        if (Target.GetDistance < _range && (Target.HealthPercent > 15 || Me.HealthPercent < 50) && Me.Level < 6)
            if (Cast(Frostbolt, true))
                return;

        // Low level FireBall
        if (Target.GetDistance < _range && !Frostbolt.KnownSpell && (Target.HealthPercent > 15 || Me.HealthPercent < 50))
            if (Cast(Fireball, true))
                return;
        
        // Use Wand
        if (!_usingWand && _iCanUseWand && ObjectManager.Target.GetDistance <= _range && !_isBackingUp && !MovementManager.InMovement)
        {
            Main.settingRange = _range;
            if (Cast(UseWand, false))
                return;
        }

        // Go in melee because nothing else to do
        if (!_usingWand && !UseWand.IsSpellUsable && Main.settingRange != _meleeRange && !_isBackingUp && Target.IsAlive)
        {
            Main.Log("Going in melee");
            Main.settingRange = _meleeRange;
            return;
        }
    }

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

        if ((_usingWand && !castEvenIfWanding) || (_isBackingUp && !s.Name.Equals("Blink")))
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
            ToolBox.StopWandWaitGCD(UseWand, Fireball);

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

    private static Spell FrostArmor = new Spell("Frost Armor");
    private static Spell Fireball = new Spell("Fireball");
    private static Spell Frostbolt = new Spell("Frostbolt");
    private static Spell FireBlast = new Spell("Fire Blast");
    private static Spell ArcaneIntellect = new Spell("Arcane Intellect");
    private static Spell FrostNova = new Spell("Frost Nova");
    private static Spell UseWand = new Spell("Shoot");
    private static Spell IcyVeins = new Spell("Icy Veins");
    private static Spell CounterSpell = new Spell("Counterspell");
    private static Spell ConeOfCold = new Spell("Cone of Cold");
    private static Spell Evocation = new Spell("Evocation");
    private static Spell Blink = new Spell("Blink");
    private static Spell ColdSnap = new Spell("Cold Snap");
    private static Spell Polymorph = new Spell("Polymorph");
    private static Spell IceBarrier = new Spell("Ice Barrier");
    private static Spell SummonWaterElemental = new Spell("Summon Water Elemental");
    private static Spell IceLance = new Spell("Ice Lance");
    private static Spell RemoveCurse = new Spell("Remove Curse");
    private static Spell IceArmor = new Spell("Ice Armor");
    private static Spell ManaShield = new Spell("Mana Shield");
    private static Spell Cannibalize = new Spell("Cannibalize");
    private static Spell WillOfTheForsaken = new Spell("Will of the Forsaken");
    private static Spell Berserking = new Spell("Berserking");
    private static Spell EscapeArtist = new Spell("Escape Artist");
    private static Spell GiftOfTheNaaru = new Spell("Gift of the Naaru");
    private static Spell ManaTap = new Spell("Mana Tap");
    private static Spell ArcaneTorrent = new Spell("Arcane Torrent");
}
