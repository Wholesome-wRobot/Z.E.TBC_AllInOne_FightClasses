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
    private static float _distanceRange = 28f;
    private static bool _usingWand = false;
    private static bool _isBackingUp = false;
    private static WoWLocalPlayer Me = ObjectManager.Me;
    private static bool _iCanUseWand = ToolBox.HaveRangedWeaponEquipped();
    private static ZEMageSettings _settings;
    private static bool _isPolymorphing;
    private static WoWUnit _polymorphedEnemy = null;
    private static bool _polymorphableEnemyInThisFight = true;

    public static void Initialize()
    {
        Main.SetRange(_distanceRange);
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
            _polymorphableEnemyInThisFight = true;
            Main.SetRange(_distanceRange);

            if (!Fight.InFight 
            && Me.InCombatFlagOnly 
            && _polymorphedEnemy != null 
            && ObjectManager.GetNumberAttackPlayer() < 1
            && _polymorphedEnemy.IsAlive)
            {
                Main.Log($"Starting fight with {_polymorphedEnemy.Name} (polymorphed)");
                Fight.InFight = false;
                Fight.CurrentTarget = _polymorphedEnemy;
                ulong _enemyGUID = _polymorphedEnemy.Guid;
                _polymorphedEnemy = null;
                Fight.StartFight(_enemyGUID);
            }
        };

        // Fight start
        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            _iCanUseWand = ToolBox.HaveRangedWeaponEquipped();
        };

        // Fight Loop
        FightEvents.OnFightLoop += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            // Do we need to backup?
            if ((ObjectManager.Target.HaveBuff("Frostbite") || ObjectManager.Target.HaveBuff("Frost Nova"))
            && ObjectManager.Target.GetDistance < 10f
            && Me.IsAlive 
            && ObjectManager.Target.IsAlive
            && !_isBackingUp
            && !Me.IsCast
            && !Main.CurrentRangeIsMelee()
            && ObjectManager.Target.HealthPercent > 5
            && !_isPolymorphing)
            {
                _isBackingUp = true;
                int limiter = 0;

                // Using CTM
                if (_settings.BackupUsingCTM)
                {
                    Vector3 position = ToolBox.BackofVector3(Me.Position, Me, 15f);
                    MovementManager.Go(PathFinder.FindPath(position), false);
                    Thread.Sleep(500);

                    // Backup loop
                    while (MovementManager.InMoveTo
                    && Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    && ObjectManager.Target.GetDistance < 15f
                    && ObjectManager.Me.IsAlive
                    && ObjectManager.Target.IsAlive
                    && (ObjectManager.Target.HaveBuff("Frostbite") || ObjectManager.Target.HaveBuff("Frost Nova"))
                    && limiter < 10)
                    {
                        // Wait follow path
                        Thread.Sleep(300);
                        limiter++;
                        if (_settings.BlinkWhenBackup)
                            Cast(Blink);
                    }
                }
                // Using Keyboard
                else
                {
                    while (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    && ObjectManager.Me.IsAlive
                    && ObjectManager.Target.IsAlive
                    && ObjectManager.Target.GetDistance < 15f
                    && limiter <= 6)
                    {
                        Move.Backward(Move.MoveAction.PressKey, 500);
                        limiter++;
                    }
                }
                _isBackingUp = false;
            }
            
            // Polymorph
            if (_settings.UsePolymorph 
            && ObjectManager.GetNumberAttackPlayer() > 1 
            && Polymorph.KnownSpell
            && !_isBackingUp 
            && _polymorphableEnemyInThisFight)
            {
                WoWUnit myNearbyPolymorphed = null;
                // Detect if a polymorph cast has succeeded
                if (_polymorphedEnemy != null)
                    myNearbyPolymorphed = ObjectManager.GetObjectWoWUnit().Find(u => u.HaveBuff("Polymorph") && u.Guid == _polymorphedEnemy.Guid);
                
                // If we don't have a polymorphed enemy
                if (myNearbyPolymorphed == null)
                {
                    _polymorphedEnemy = null;
                    _isPolymorphing = true;
                    WoWUnit firstTarget = ObjectManager.Target;
                    WoWUnit potentialPolymorphTarget = null;

                    // Select our attackers one by one for potential polymorphs
                    foreach (WoWUnit enemy in ObjectManager.GetUnitAttackPlayer())
                    {
                        Interact.InteractGameObject(enemy.GetBaseAddress);

                        if ((enemy.CreatureTypeTarget == "Beast" || enemy.CreatureTypeTarget == "Humanoid") 
                        && enemy.Guid != firstTarget.Guid)
                        {
                            potentialPolymorphTarget = enemy;
                            break;
                        }
                    }

                    if (potentialPolymorphTarget == null)
                        _polymorphableEnemyInThisFight = false;

                    // Polymorph cast
                    if (potentialPolymorphTarget != null && _polymorphedEnemy == null)
                    {
                        Interact.InteractGameObject(potentialPolymorphTarget.GetBaseAddress);
                        Cast(Polymorph);
                        _polymorphedEnemy = potentialPolymorphTarget;
                        Usefuls.WaitIsCasting();
                    }

                    // Get back to actual target
                    Interact.InteractGameObject(firstTarget.GetBaseAddress);
                    _isPolymorphing = false;
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
                    if (_polymorphedEnemy != null && !ObjectManager.Me.InCombatFlagOnly)
                        _polymorphedEnemy = null;

                    if (!Fight.InFight && !ObjectManager.Me.InCombatFlagOnly && !Me.IsMounted)
                        BuffRotation();

                    if (Fight.InFight 
                        && ObjectManager.Me.Target > 0UL 
                        && ObjectManager.Target.IsAttackable 
                        && ObjectManager.Target.IsAlive
                        && !_isBackingUp
                        && !_isPolymorphing)
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
        if (_target.GetDistance < _distanceRange && Me.Level >= 6 && (_target.HealthPercent > _settings.WandThreshold
            || ObjectManager.GetNumberAttackPlayer() > 1 || Me.HealthPercent < 30 || !_iCanUseWand))
            if (Cast(Frostbolt))
                return;

        // Low level Frost Bolt
        if (_target.GetDistance < _distanceRange && _target.HealthPercent > 30 && Me.Level < 6)
            if (Cast(Frostbolt))
                return;

        // Low level FireBall
        if (_target.GetDistance < _distanceRange && !Frostbolt.KnownSpell && _target.HealthPercent > 30)
            if (Cast(Fireball))
                return;
    }

    internal static void CombatRotation()
    {
        Lua.LuaDoString("PetAttack();", false);
        WoWUnit Target = ObjectManager.Target;
        _usingWand = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(5019); " +
            "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");

        // Stop wand use on multipull
        if (_iCanUseWand && ObjectManager.GetNumberAttackPlayer() > 1)
            _iCanUseWand = false;

        // Remove Curse
        if (ToolBox.HasCurseDebuff())
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
        if (!Me.HaveBuff("Mana Shield") && ((Me.HealthPercent < 30 && Me.ManaPercentage > 50) || Me.HealthPercent < 10))
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
        if (Target.GetDistance < 6f 
            && Target.HealthPercent > 10 
            && !Target.HaveBuff("Frostbite")
            && _polymorphedEnemy == null)
            if (Cast(FrostNova))
                return;

        // Fire Blast
        if (Target.GetDistance < 20f 
            && Target.HealthPercent <= _settings.FireblastThreshold 
            && !Target.HaveBuff("Frostbite") && !Target.HaveBuff("Frost Nova"))
            if (Cast(FireBlast))
                return;

        // Cone of Cold
        if (Target.GetDistance < 10 
            && _settings.UseConeOfCold 
            && !_isBackingUp 
            && !MovementManager.InMovement
            && _polymorphedEnemy == null)
            if (Cast(ConeOfCold))
                return;

        // Frost Bolt
        if (Target.GetDistance < _distanceRange 
            && Me.Level >= 6 
            && (Target.HealthPercent > _settings.WandThreshold|| ObjectManager.GetNumberAttackPlayer() > 1 || Me.HealthPercent < 40 || !_iCanUseWand))
            if (Cast(Frostbolt, true))
                return;

        // Low level Frost Bolt
        if (Target.GetDistance < _distanceRange 
            && (Target.HealthPercent > 15 || Me.HealthPercent < 50) 
            && Me.Level < 6)
            if (Cast(Frostbolt, true))
                return;

        // Low level FireBall
        if (Target.GetDistance < _distanceRange 
            && !Frostbolt.KnownSpell 
            && (Target.HealthPercent > 15 || Me.HealthPercent < 50))
            if (Cast(Fireball, true))
                return;
        
        // Use Wand
        if (!_usingWand 
            && _iCanUseWand 
            && ObjectManager.Target.GetDistance <= _distanceRange 
            && !_isBackingUp 
            && !MovementManager.InMovement)
        {
            Main.SetRange(_distanceRange);
            if (Cast(UseWand, false))
                return;
        }

        // Go in melee because nothing else to do
        if (!_usingWand 
            && !UseWand.IsSpellUsable 
            && !Main.CurrentRangeIsMelee()
            && !_isBackingUp 
            && Target.IsAlive)
        {
            Main.Log("Going in melee");
            Main.SetRangeToMelee();
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
