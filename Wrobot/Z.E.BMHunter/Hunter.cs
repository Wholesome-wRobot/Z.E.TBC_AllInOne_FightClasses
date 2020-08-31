using System;
using System.ComponentModel;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public static class Hunter
{
    private static readonly float _distanceRange = 28f;
    private static WoWLocalPlayer Me = ObjectManager.Me;
    private static HunterFoodManager _foodManager = new HunterFoodManager();
    private static readonly BackgroundWorker _petPulseThread = new BackgroundWorker();
    internal static ZEBMHunterSettings _settings;
    public static bool _autoshotRepeating;
    public static bool RangeCheck;
    private static bool _isBackingUp = false;
    private static int _backupAttempts = 0;
    private static int _steadyShotSleep = 0;
    private static bool _canOnlyMelee = false;

    public static void Initialize()
    {
        Main.Log("Initialized");
        _petPulseThread.DoWork += PetThread;
        _petPulseThread.RunWorkerAsync();
        ZEBMHunterSettings.Load();
        _settings = ZEBMHunterSettings.CurrentSetting;
        Talents.InitTalents(_settings.AssignTalents, _settings.UseDefaultTalents, _settings.TalentCodes);

        // Set Steady Shot delay
        if (_settings.RangedWeaponSpeed > 2000)
        {
            _steadyShotSleep = _settings.RangedWeaponSpeed - 1600;
        }
        else
        {
            _steadyShotSleep = 500;
        }
        Main.LogDebug("Steady Shot delay set to : " + _steadyShotSleep.ToString() + "ms");

        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            if (ObjectManager.Target.GetDistance >= 13f && !AutoShot.IsSpellUsable && !_isBackingUp)
                _canOnlyMelee = true;
            else
                _canOnlyMelee = false;
        };

        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _isBackingUp = false;
            _backupAttempts = 0;
            _autoshotRepeating = false;
            _canOnlyMelee = false;
        };

        FightEvents.OnFightLoop += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            // Do we need to backup?
            if (ObjectManager.Target.GetDistance < 10f && ObjectManager.Target.IsTargetingMyPet
            && !MovementManager.InMovement
            && Me.IsAlive 
            && ObjectManager.Target.IsAlive
            && !ObjectManager.Pet.HaveBuff("Pacifying Dust")  && !_canOnlyMelee
            && !ObjectManager.Pet.IsStunned  && !_isBackingUp 
            && !Me.IsCast  && _settings.BackupFromMelee)
            {
                // Stop trying if we reached the max amount of attempts
                if (_backupAttempts >= _settings.MaxBackupAttempts)
                {
                    Main.Log($"Backup failed after {_backupAttempts} attempts. Going in melee");
                    _canOnlyMelee = true;
                    return;
                }

                _isBackingUp = true;

                // Using CTM
                if (_settings.BackupUsingCTM)
                {
                    Vector3 position = ToolBox.BackofVector3(Me.Position, Me, 12f);
                    MovementManager.Go(PathFinder.FindPath(position), false);
                    Thread.Sleep(500);

                    // Backup loop
                    int limiter = 0;
                    while (MovementManager.InMoveTo 
                    && Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    && ObjectManager.Me.IsAlive
                    && ObjectManager.Target.GetDistance < 10f
                    && limiter < 10)
                    {
                        // Wait follow path
                        Thread.Sleep(300);
                        limiter++;
                    }
                }
                // Using Keyboard
                else
                {
                    int limiter = 0;
                    while (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    && ObjectManager.Me.IsAlive
                    && ObjectManager.Target.GetDistance < 10f
                    && limiter <= 6)
                    {
                        Move.Backward(Move.MoveAction.PressKey, 500);
                        limiter++;
                    }
                }

                _backupAttempts++;
                Main.Log($"Backup attempt : {_backupAttempts}");
                _isBackingUp = false;

                if (RaptorStrikeOn())
                    Cast(RaptorStrike);
                ReenableAutoshot();
            }
        };

        Rotation();
    }

    // Pet thread
    private static void PetThread(object sender, DoWorkEventArgs args)
    {
        while (Main._isLaunched)
        {
            try
            {
                if (Conditions.InGameAndConnectedAndProductStartedNotInPause && !Me.IsOnTaxi && Me.IsAlive
                    && ObjectManager.Pet.IsValid && !Main.HMPrunningAway)
                {
                    // Pet Growl
                    if (ObjectManager.Target.Target == Me.Guid && Me.InCombatFlagOnly && !_settings.AutoGrowl
                        && !ObjectManager.Pet.HaveBuff("Feed Pet Effect"))
                        ToolBox.PetSpellCast("Growl");
                }
            }
            catch (Exception arg)
            {
                Logging.WriteError(string.Concat(arg), true);
            }
            Thread.Sleep(300);
        }
    }


    public static void Dispose()
    {
        Main.Log("Stop in progress.");
        _petPulseThread.DoWork -= PetThread;
        _petPulseThread.Dispose();
    }

    internal static void Rotation()
	{
        Main.Log("Started");
		while (Main._isLaunched)
		{
			try
			{
				if (!Products.InPause && !Me.IsDeadMe && !Main.HMPrunningAway)
				{
                    if (_canOnlyMelee)
                        Main.SetRangeToMelee();
                    else
                        Main.SetRange(_distanceRange);

                    PetManager();

                    // Switch Auto Growl
                    if (ObjectManager.Pet.IsValid)
                    {
                        ToolBox.TogglePetSpellAuto("Growl", _settings.AutoGrowl);
                    }

                    // Feed
                    if (Lua.LuaDoString<int>("happiness, damagePercentage, loyaltyRate = GetPetHappiness() return happiness", "") < 3 
                        && !Fight.InFight && _settings.FeedPet)
						Feed();

                    // Pet attack
					if (Fight.InFight && Me.Target > 0UL && ObjectManager.Target.IsAttackable 
                        && !ObjectManager.Pet.HaveBuff("Feed Pet Effect") && ObjectManager.Pet.Target != Me.Target)
						Lua.LuaDoString("PetAttack();", false);

                    // Aspect of the Cheetah
                    if (!Me.IsMounted && !Fight.InFight && !Me.HaveBuff("Aspect of the Cheetah")
                        && MovementManager.InMoveTo && Me.ManaPercentage > 60f)
                        Cast(AspectCheetah);

					if (Fight.InFight && Me.Target > 0UL && ObjectManager.Target.IsAttackable)
						CombatRotation();
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

    internal static void CombatRotation()
    {
        WoWUnit Target = ObjectManager.Target;

        if (Target.GetDistance < 10f && !_isBackingUp)
            ToolBox.CheckAutoAttack(Attack);

        if (Target.GetDistance > 10f && !_isBackingUp)
            ReenableAutoshot();

        if (Target.GetDistance < 13f && !ZEBMHunterSettings.CurrentSetting.BackupFromMelee)
            _canOnlyMelee = true;

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

        // Blood Fury
        if (Target.HealthPercent > 70)
            if (Cast(BloodFury))
                return;

        // Berserking
        if (Target.HealthPercent > 70)
            if (Cast(Berserking))
                return;

        // Stoneform
        if (ToolBox.HasPoisonDebuff() || ToolBox.HasDiseaseDebuff() || Me.HaveBuff("Bleed"))
            if (Cast(Stoneform))
                return;

        // Warstomp
        if (ObjectManager.GetNumberAttackPlayer() > 1 && Target.GetDistance < 8)
            if (Cast(WarStomp))
                return;

        // Aspect of the viper
        if (!Me.HaveBuff("Aspect of the Viper") && Me.ManaPercentage < 30)
            if (Cast(AspectViper))
                return;

        // Aspect of the Hawk
        if (!Me.HaveBuff("Aspect of the Hawk") && (Me.ManaPercentage > 90 || Me.HaveBuff("Aspect of the Cheetah"))
            || (!Me.HaveBuff("Aspect of the Hawk") && !Me.HaveBuff("Aspect of the Cheetah") && !Me.HaveBuff("Aspect of the Viper")))
            if (Cast(AspectHawk))
                return;

        // Aspect of the Monkey
        if (!Me.HaveBuff("Aspect of the Monkey") && !AspectHawk.KnownSpell)
            if (Cast(AspectMonkey))
                return;

        // Disengage
        if (ObjectManager.Pet.Target == Me.Target && Target.Target == Me.Guid && Target.GetDistance < 10 && !_isBackingUp)
            if (Cast(Disengage))
                return;

        // Bestial Wrath
        if (Target.GetDistance < 34f && Target.HealthPercent >= 60 && Me.ManaPercentage > 10 && BestialWrath.IsSpellUsable
        && ((_settings.BestialWrathOnMulti && ObjectManager.GetUnitAttackPlayer().Count > 1) || !_settings.BestialWrathOnMulti))
            if (Cast(BestialWrath))
                return;

        // Rapid Fire
        if ( Target.GetDistance < 34f && Target.HealthPercent >= 80.0
            && ((_settings.RapidFireOnMulti && ObjectManager.GetUnitAttackPlayer().Count > 1) || !_settings.RapidFireOnMulti))
            if (Cast(RapidFire))
                return;

        // Kill Command
        if (Cast(KillCommand))
            return;

        // Raptor Strike
        if (Target.GetDistance < 6f && !RaptorStrikeOn())
            if (Cast(RaptorStrike))
                return;

        // Mongoose Bite
        if (Target.GetDistance < 6f)
            if (Cast(MongooseBite))
                return;

        // Feign Death
        if (Me.HealthPercent < 20)
            if (Cast(FeignDeath))
            {
                Fight.StopFight();
                return;
            }

        // Freezing Trap
        if (ObjectManager.Pet.HaveBuff("Mend Pet") && ObjectManager.GetUnitAttackPlayer().Count > 1 && _settings.UseFreezingTrap)
            if (Cast(FreezingTrap))
                return;

        // Mend Pet
        if (ObjectManager.Pet.IsValid && ObjectManager.Pet.HealthPercent <= 30.0 
            && !ObjectManager.Pet.HaveBuff("Mend Pet"))
            if (Cast(MendPet))
                return;
    
        // Hunter's Mark
        if (ObjectManager.Pet.IsValid && !HuntersMark.TargetHaveBuff && Target.GetDistance > 13f && Target.IsAlive)
            if (Cast(HuntersMark))
                return;

        // Steady Shot
        if (SteadyShot.KnownSpell && SteadyShot.IsSpellUsable && Me.ManaPercentage > 30 && SteadyShot.IsDistanceGood && !_isBackingUp)
        {
            SteadyShot.Launch();
            Thread.Sleep(_steadyShotSleep);
        }

        // Serpent Sting
        if (!Target.HaveBuff("Serpent Sting") 
            && Target.GetDistance < 34f 
            && ToolBox.CanBleed(Me.TargetObject) 
            && Target.HealthPercent >= 80 
            && Me.ManaPercentage > 50u 
            && !SteadyShot.KnownSpell
            && Target.GetDistance > 13f)
            if (Cast(SerpentSting))
                return;

        // Intimidation
        if (Target.GetDistance < 34f && Target.GetDistance > 10f && Target.HealthPercent >= 20 
            && Me.ManaPercentage > 10)
            if (Cast(Intimidation))
                return;

        // Arcane Shot
        if (Target.GetDistance < 34f && Target.HealthPercent >= 30 && Me.ManaPercentage > 80
            && !SteadyShot.KnownSpell)
            if (Cast(ArcaneShot))
                return;
    }

    public static void Feed()
    {
        if (ObjectManager.Pet.IsAlive && !Me.IsCast && !ObjectManager.Pet.HaveBuff("Feed Pet Effect"))
        {
            _foodManager.FeedPet();
            Thread.Sleep(400);
        }
    }

    internal static void PetManager()
    {
        if (!Me.IsDeadMe || !Me.IsMounted)
        {
            // Call Pet
            if (!ObjectManager.Pet.IsValid && CallPet.KnownSpell && !Me.IsMounted && CallPet.IsSpellUsable)
            {
                CallPet.Launch();
                Thread.Sleep(Usefuls.Latency + 1000);
            }

            // Revive Pet
            if (ObjectManager.Pet.IsDead && RevivePet.KnownSpell && !Me.IsMounted && RevivePet.IsSpellUsable)
            {
                RevivePet.Launch();
                Thread.Sleep(Usefuls.Latency + 1000);
                Usefuls.WaitIsCasting();
            }

            // Mend Pet
            if (ObjectManager.Pet.IsAlive && ObjectManager.Pet.IsValid && !ObjectManager.Pet.HaveBuff("Mend Pet")
                && Me.IsAlive && MendPet.KnownSpell && MendPet.IsDistanceGood && ObjectManager.Pet.HealthPercent <= 60
                && MendPet.IsSpellUsable)
            {
                MendPet.Launch();
                Thread.Sleep(Usefuls.Latency + 1000);
            }
        }
    }

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

    private static bool RaptorStrikeOn()
    {
        return Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Raptor Strike') then isAutoRepeat = true end", "isAutoRepeat");
    }

    private static void ReenableAutoshot()
    {
        _autoshotRepeating = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(75); " +
               "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");
        if (!_autoshotRepeating)
        {
            Main.LogDebug("Re-enabling auto shot");
            AutoShot.Launch();
        }
    }

    public static void ShowConfiguration()
    {
        ZEBMHunterSettings.Load();
        ZEBMHunterSettings.CurrentSetting.ToForm();
        ZEBMHunterSettings.CurrentSetting.Save();
    }

    private static Spell RevivePet = new Spell("Revive Pet");
    private static Spell CallPet = new Spell("Call Pet");
    private static Spell MendPet = new Spell("Mend Pet");
    private static Spell AspectHawk = new Spell("Aspect of the Hawk");
    private static Spell AspectCheetah = new Spell("Aspect of the Cheetah");
    private static Spell AspectMonkey = new Spell("Aspect of the Monkey");
    private static Spell AspectViper = new Spell("Aspect of the Viper");
    private static Spell HuntersMark = new Spell("Hunter's Mark");
    private static Spell ConcussiveShot = new Spell("Concussive Shot");
    private static Spell RaptorStrike = new Spell("Raptor Strike");
    private static Spell MongooseBite = new Spell("Mongoose Bite");
    private static Spell WingClip = new Spell("Wing Clip");
    private static Spell SerpentSting = new Spell("Serpent Sting");
    private static Spell ArcaneShot = new Spell("Arcane Shot");
    private static Spell AutoShot = new Spell("Auto Shot");
    private static Spell RapidFire = new Spell("Rapid Fire");
    private static Spell Intimidation = new Spell("Intimidation");
    private static Spell BestialWrath = new Spell("Bestial Wrath");
    private static Spell FeignDeath = new Spell("Feign Death");
    private static Spell FreezingTrap = new Spell("Freezing Trap");
    private static Spell SteadyShot = new Spell("Steady Shot");
    private static Spell KillCommand = new Spell("Kill Command");
    private static Spell Disengage = new Spell("Disengage");
    private static Spell Attack = new Spell("Attack");
    private static Spell BloodFury = new Spell("Blood Fury");
    private static Spell Berserking = new Spell("Berserking");
    private static Spell WarStomp = new Spell("War Stomp");
    private static Spell Stoneform = new Spell("Stoneform");
    private static Spell GiftOfTheNaaru = new Spell("Gift of the Naaru");
    private static Spell ManaTap = new Spell("Mana Tap");
    private static Spell ArcaneTorrent = new Spell("Arcane Torrent");
}
