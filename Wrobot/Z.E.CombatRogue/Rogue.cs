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
using robotManager.FiniteStateMachine;

public static class Rogue
{
    private static float _meleRange = Main.settingRange;
    private static float _pullRange = 25f;
    internal static Stopwatch _pullMeleeTimer = new Stopwatch();
    internal static Stopwatch _meleeTimer = new Stopwatch();
    internal static Stopwatch _stealthApproachTimer = new Stopwatch();
    private static WoWLocalPlayer Me = ObjectManager.Me;
    internal static ZERogueSettings _settings;
    private static bool _fightingACaster = false;
    private static List<string> _casterEnemies = new List<string>();
    private static bool _pullFromAfar = false;
    private static bool _isStealthApproching;
    public static uint MHPoison;
    public static uint OHPoison;
    private static string _myBestBandage = null;
    private static readonly BackgroundWorker _pulseThread = new BackgroundWorker();

    public static void Initialize()
    {
        Main.Log("Initialized");
        ZERogueSettings.Load();
        _settings = ZERogueSettings.CurrentSetting;
        Talents.InitTalents(_settings.AssignTalents, _settings.UseDefaultTalents, _settings.TalentCodes);

        // Fight End
        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _meleeTimer.Reset();
            _pullMeleeTimer.Reset();
            _stealthApproachTimer.Reset();
            _fightingACaster = false;
            _pullFromAfar = false;
            _isStealthApproching = false;
            _myBestBandage = null;
            Main.settingRange = _meleRange;
        };

        // Fight Start
        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            _myBestBandage = ToolBox.GetBestMatchingItem(Bandages());
            if (_myBestBandage != null)
                Main.LogDebug("Found best bandage : " + _myBestBandage);
        };

        // We override movement to target when approaching in Stealth
        MovementEvents.OnMoveToPulse += (Vector3 point, CancelEventArgs cancelable) =>
        {
            if (_isStealthApproching &&
            !point.ToString().Equals(ToolBox.BackofVector3(ObjectManager.Target.Position, ObjectManager.Target, 2.5f).ToString()))
                cancelable.Cancel = true;
        };

        // Fight Loop - Go behind target when gouged
        FightEvents.OnFightLoop += (WoWUnit unit, CancelEventArgs cancelable) =>
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
        OthersEvents.OnAddBlackListGuid += (ulong guid, int timeInMilisec, bool isSessionBlacklist, CancelEventArgs cancelable) =>
        {
            if (Me.HaveBuff("Stealth") && !_pullFromAfar)
            {
                Main.LogDebug("BL : " + guid + " ms : " + timeInMilisec + " is session: " + isSessionBlacklist);
                Main.Log("Cancelling Blacklist event");
                cancelable.Cancel = true;
            }
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
                    if (ObjectManager.GetNumberAttackPlayer() < 1)
                    {
                        BuffRotation();
                    }

                    // Pull & Combat rotation
                    if (Fight.InFight && ObjectManager.Me.Target > 0UL && ObjectManager.Target.IsAttackable
                        && ObjectManager.Target.IsAlive)
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
            PoisonWeapon();

            // Sprint
            if (_settings.SprintWhenAvail && Me.HealthPercent > 80 && MovementManager.InMovement && Sprint.IsSpellUsable
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

    internal static void Pull()
    {
        // Check if surrounding enemies
        if (ObjectManager.Target.GetDistance < _pullRange && !_pullFromAfar)
            _pullFromAfar = ToolBox.CheckIfEnemiesOnPull(ObjectManager.Target, _pullRange);

        // Pull from afar
        if ((_pullFromAfar && _pullMeleeTimer.ElapsedMilliseconds < 5000) || _settings.AlwaysPull
            && ObjectManager.Target.GetDistance <= _pullRange)
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

        // Stealth
        if (!Me.HaveBuff("Stealth") && !_pullFromAfar && ObjectManager.Target.GetDistance > 15f
            && ObjectManager.Target.GetDistance < 25f && _settings.StealthApproach && Backstab.KnownSpell
            && (!ToolBox.HasPoisonDebuff() || _settings.StealthWhenPoisoned))
            if (Cast(Stealth))
                return;

        // Un-Stealth
        if (Me.HaveBuff("Stealth") && _pullFromAfar && ObjectManager.Target.GetDistance > 15f)
            if (Cast(Stealth))
                return;

        // Stealth approach
        if (Me.HaveBuff("Stealth") && ObjectManager.Target.GetDistance > 3f && !_isStealthApproching && !_pullFromAfar)
        {
            Main.settingRange = _meleRange;
            _stealthApproachTimer.Start();
            _isStealthApproching = true;
            if (ObjectManager.Me.IsAlive && ObjectManager.Target.IsAlive)
            {

                while (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                && (ObjectManager.Target.GetDistance > 4f)
                && !ToolBox.CheckIfEnemiesOnPull(ObjectManager.Target, _pullRange) && Fight.InFight
                && _stealthApproachTimer.ElapsedMilliseconds <= 15000 && Me.HaveBuff("Stealth"))
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
                    if (_settings.UseGarrote)
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
                    Main.Log("_stealthApproachTimer time out");
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

    internal static void CombatRotation()
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

        if ((_shouldBeInterrupted || _meleeTimer.ElapsedMilliseconds > 5000) && Main.settingRange != _meleRange)
        {
            Main.LogDebug("Going in Melee range 2");
            Main.settingRange = _meleRange;
            _meleeTimer.Stop();
        }
        
        // Vanish
        /*if (Me.HealthPercent < 10)
            if (Cast(Vanish))
            {
                Main.Log("Stopping Fight after vanish");
                Fight.StopFight();
                return;
            }*/

        // Kick interrupt
        if (_shouldBeInterrupted)
        {
            Thread.Sleep(Main._humanReflexTime);
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
        if (Riposte.IsSpellUsable && (Target.CreatureTypeTarget.Equals("Humanoid") || _settings.RiposteAll))
            if (Cast(Riposte))
                return;

        // Bandage
        if (Target.HaveBuff("Blind"))
        {
            MovementManager.StopMoveTo(true, 500);
            ItemsManager.UseItemByNameOrId(_myBestBandage);
            Main.Log("Using " + _myBestBandage);
            Usefuls.WaitIsCasting();
            return;
        }

        // Blind
        if (Me.HealthPercent < 40 && !ToolBox.HasDebuff("Recently Bandaged") && _myBestBandage != null
            && _settings.UseBlindBandage)
            if (Cast(Blind))
                return;

        // Evasion
        if (ObjectManager.GetNumberAttackPlayer() > 1 || (Me.HealthPercent < 30 && !Me.HaveBuff("Evasion") && Target.HealthPercent > 50))
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
        if ((Me.ComboPoint > 0 && Target.HealthPercent < 30)
            || (Me.ComboPoint > 1 && Target.HealthPercent < 45)
            || (Me.ComboPoint > 2 && Target.HealthPercent < 60)
            || (Me.ComboPoint > 3 && Target.HealthPercent < 70))
            if (Cast(Eviscerate))
                return;

        // GhostlyStrike
        if (Me.ComboPoint < 5 && !IsTargetStunned() &&
            (!_fightingACaster || !Kick.KnownSpell ||
            (Me.Energy > (ToolBox.GetSpellCost("Ghostly Strike") + ToolBox.GetSpellCost("Kick")))))
            if (Cast(GhostlyStrike))
                return;

        // Hemohrrage
        if (Me.ComboPoint < 5 && !IsTargetStunned() &&
            (!_fightingACaster || !Kick.KnownSpell ||
            (Me.Energy > (ToolBox.GetSpellCost("Hemorrhage") + ToolBox.GetSpellCost("Kick")))))
            if (Cast(Hemorrhage))
                return;

        // Sinister Strike
        if (Me.ComboPoint < 5 && !IsTargetStunned() && 
            (!_fightingACaster || !Kick.KnownSpell || 
            (Me.Energy > (ToolBox.GetSpellCost("Sinister Strike") + ToolBox.GetSpellCost("Kick")))))
            if (Cast(SinisterStrike))
                return;
    }

    public static void ShowConfiguration()
    {
        ZERogueSettings.Load();
        ZERogueSettings.CurrentSetting.ToForm();
        ZERogueSettings.CurrentSetting.Save();
    }

    private static Spell Attack = new Spell("Attack");
    private static Spell Shoot = new Spell("Shoot");
    private static Spell Throw = new Spell("Throw");
    private static Spell Eviscerate = new Spell("Eviscerate");
    private static Spell SinisterStrike = new Spell("Sinister Strike");
    private static Spell Stealth = new Spell("Stealth");
    private static Spell Backstab = new Spell("Backstab");
    private static Spell Gouge = new Spell("Gouge");
    private static Spell Evasion = new Spell("Evasion");
    private static Spell Kick = new Spell("Kick");
    private static Spell Garrote = new Spell("Garrote");
    private static Spell SliceAndDice = new Spell("Slice and Dice");
    private static Spell Vanish = new Spell("Vanish");
    private static Spell CheapShot = new Spell("Cheap Shot");
    private static Spell Riposte = new Spell("Riposte");
    private static Spell BladeFlurry = new Spell("Blade Flurry");
    private static Spell AdrenalineRush = new Spell("Adrenaline Rush");
    private static Spell Sprint = new Spell("Sprint");
    private static Spell CloakOfShadows = new Spell("Cloak of Shadows");
    private static Spell Blind = new Spell("Blind");
    private static Spell KidneyShot = new Spell("Kidney Shot");
    private static Spell Hemorrhage = new Spell("Hemorrhage");
    private static Spell GhostlyStrike = new Spell("Ghostly Strike");
    private static Spell Cannibalize = new Spell("Cannibalize");
    private static Spell WillOfTheForsaken = new Spell("Will of the Forsaken");
    private static Spell BloodFury = new Spell("Blood Fury");
    private static Spell Berserking = new Spell("Berserking");
    private static Spell Stoneform = new Spell("Stoneform");
    private static Spell EscapeArtist = new Spell("Escape Artist");
    private static Spell ManaTap = new Spell("Mana Tap");
    private static Spell ArcaneTorrent = new Spell("Arcane Torrent");

    internal static bool Cast(Spell s)
    {
        if (!s.KnownSpell)
            return false;

        CombatDebug("In cast for " + s.Name);
        if (!s.IsSpellUsable || Me.IsCast)
            return false;

        s.Launch();
        return true;
    }

    private static void CombatDebug(string s)
    {
        if (_settings.ActivateCombatDebug)
            Main.CombatDebug(s);
    }

    public static List<string> Bandages()
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

    private static void ToggleAutoAttack(bool activate)
    {
        bool _autoAttacking = Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Attack') " +
            "then isAutoRepeat = true end", "isAutoRepeat");

        if (!_autoAttacking && activate && !ObjectManager.Target.HaveBuff("Gouge") 
            && (!ObjectManager.Target.HaveBuff("Blind") || ToolBox.HasDebuff("Recently Bandaged")))
        {
            Main.Log("Turning auto attack ON");
            ToolBox.CheckAutoAttack(Attack);
        }

        if (!activate && _autoAttacking)
        {
            Main.Log("Turning auto attack OFF");
            Attack.Launch();
        }
    }

    private static bool IsTargetStunned()
    {
        return ObjectManager.Target.HaveBuff("Gouge") || ObjectManager.Target.HaveBuff("Cheap Shot");
    }

    private static bool HaveDaggerInMH()
    {
        return ToolBox.GetMHWeaponType().Equals("Daggers");
    }

    private static void PoisonWeapon()
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

    private static Dictionary<int, uint> InstantPoisonDictionary = new Dictionary<int, uint>
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

    private static Dictionary<int, uint> DeadlyPoisonDictionary = new Dictionary<int, uint>
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
