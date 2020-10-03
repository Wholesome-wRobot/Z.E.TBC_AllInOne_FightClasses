using System;
using System.ComponentModel;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using WholesomeTBCAIO.Helpers;
using WholesomeTBCAIO.Settings;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeTBCAIO.Rotations.Hunter
{
    public class Hunter : IClassRotation
    {
        public static HunterSettings settings;

        protected WoWLocalPlayer Me = ObjectManager.Me;
        protected HunterFoodManager _foodManager = new HunterFoodManager();
        protected BackgroundWorker _petPulseThread = new BackgroundWorker();

        protected float _distanceRange = 28f;
        protected bool _autoshotRepeating;
        protected bool RangeCheck;
        protected bool _isBackingUp = false;
        protected int _backupAttempts = 0;
        protected int _steadyShotSleep = 0;
        protected bool _canOnlyMelee = false;

        protected Hunter specialization;

        public void Initialize(IClassRotation specialization)
        {
            Logger.Log("Initialized");
            settings = HunterSettings.Current;

            this.specialization = specialization as Hunter;
            Talents.InitTalents(settings);

            _petPulseThread.DoWork += PetThread;
            _petPulseThread.RunWorkerAsync();

            // Set Steady Shot delay
            if (settings.RangedWeaponSpeed > 2000)
            {
                _steadyShotSleep = settings.RangedWeaponSpeed - 1600;
            }
            else
            {
                _steadyShotSleep = 500;
            }
            Logger.LogDebug("Steady Shot delay set to : " + _steadyShotSleep.ToString() + "ms");

            FightEvents.OnFightStart += (unit, cancelable) =>
            {
                if (ObjectManager.Target.GetDistance >= 13f && !AutoShot.IsSpellUsable && !_isBackingUp)
                    _canOnlyMelee = true;
                else
                    _canOnlyMelee = false;
            };

            FightEvents.OnFightEnd += (guid) =>
            {
                _isBackingUp = false;
                _backupAttempts = 0;
                _autoshotRepeating = false;
                _canOnlyMelee = false;
            };

            FightEvents.OnFightLoop += (unit, cancelable) =>
            {
                // Do we need to backup?
                if (ObjectManager.Target.GetDistance < 10f && ObjectManager.Target.IsTargetingMyPet
                    && !MovementManager.InMovement
                    && Me.IsAlive
                    && ObjectManager.Target.IsAlive
                    && !ObjectManager.Pet.HaveBuff("Pacifying Dust") && !_canOnlyMelee
                    && !ObjectManager.Pet.IsStunned && !_isBackingUp
                    && !Me.IsCast && settings.BackupFromMelee)
                {
                    // Stop trying if we reached the max amount of attempts
                    if (_backupAttempts >= settings.MaxBackupAttempts)
                    {
                        Logger.Log($"Backup failed after {_backupAttempts} attempts. Going in melee");
                        _canOnlyMelee = true;
                        return;
                    }

                    _isBackingUp = true;

                    // Using CTM
                    if (settings.BackupUsingCTM)
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
                    Logger.Log($"Backup attempt : {_backupAttempts}");
                    _isBackingUp = false;

                    if (RaptorStrikeOn())
                        Cast(RaptorStrike);
                    ReenableAutoshot();
                }
            };

            Rotation();
        }

        // Pet thread
        private void PetThread(object sender, DoWorkEventArgs args)
        {
            while (Main.isLaunched)
            {
                try
                {
                    if (Conditions.InGameAndConnectedAndProductStartedNotInPause && !Me.IsOnTaxi && Me.IsAlive
                        && ObjectManager.Pet.IsValid && !Main.HMPrunningAway)
                    {
                        // Pet Growl
                        if (ObjectManager.Target.Target == Me.Guid && Me.InCombatFlagOnly && !settings.AutoGrowl
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

        public void Dispose()
        {
            Logger.Log("Stop in progress.");
            _petPulseThread.DoWork -= PetThread;
            _petPulseThread.Dispose();
        }

        private void Rotation()
        {
            Logger.Log("Started");
            while (Main.isLaunched)
            {
                try
                {
                    if (!Products.InPause 
                        && !Me.IsDeadMe 
                        && !Main.HMPrunningAway)
                    {
                        if (_canOnlyMelee)
                            RangeManager.SetRangeToMelee();
                        else
                            RangeManager.SetRange(_distanceRange);

                        PetManager();

                        // Switch Auto Growl
                        if (ObjectManager.Pet.IsValid)
                        {
                            ToolBox.TogglePetSpellAuto("Growl", settings.AutoGrowl);
                        }

                        // Feed
                        if (Lua.LuaDoString<int>("happiness, damagePercentage, loyaltyRate = GetPetHappiness() return happiness", "") < 3
                            && !Fight.InFight && settings.FeedPet)
                            Feed();

                        // Pet attack
                        if (Fight.InFight && Me.Target > 0UL && ObjectManager.Target.IsAttackable
                            && !ObjectManager.Pet.HaveBuff("Feed Pet Effect") && ObjectManager.Pet.Target != Me.Target)
                            Lua.LuaDoString("PetAttack();", false);

                        // Aspect of the Cheetah
                        if (!Me.IsMounted && !Fight.InFight
                            && !Me.HaveBuff("Aspect of the Cheetah")
                            && MovementManager.InMoveTo &&
                            Me.ManaPercentage > 60f
                            && settings.UseAspectOfTheCheetah)
                            Cast(AspectCheetah);

                        if (Fight.InFight && Me.Target > 0UL && ObjectManager.Target.IsAttackable)
                            specialization.CombatRotation();
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

        protected virtual void CombatRotation()
        {
            WoWUnit Target = ObjectManager.Target;

            if (Target.GetDistance < 10f && !_isBackingUp)
                ToolBox.CheckAutoAttack(Attack);

            if (Target.GetDistance > 10f && !_isBackingUp)
                ReenableAutoshot();

            if (Target.GetDistance < 13f && !settings.BackupFromMelee)
                _canOnlyMelee = true;

            // Mana Tap
            if (Target.Mana > 0 && Target.ManaPercentage > 10)
                if (Cast(ManaTap))
                    return;

            // Arcane Torrent
            if (Me.HaveBuff("Mana Tap") && Me.ManaPercentage < 50
                || Target.IsCast && Target.GetDistance < 8)
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
            if (!Me.HaveBuff("Aspect of the Hawk")
                && (Me.ManaPercentage > 90 || Me.HaveBuff("Aspect of the Cheetah"))
                || !Me.HaveBuff("Aspect of the Hawk") && !Me.HaveBuff("Aspect of the Cheetah") && !Me.HaveBuff("Aspect of the Viper"))
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
            && (settings.BestialWrathOnMulti && ObjectManager.GetUnitAttackPlayer().Count > 1 || !settings.BestialWrathOnMulti))
                if (Cast(BestialWrath))
                    return;

            // Rapid Fire
            if (Target.GetDistance < 34f && Target.HealthPercent >= 80.0
                && (settings.RapidFireOnMulti && ObjectManager.GetUnitAttackPlayer().Count > 1 || !settings.RapidFireOnMulti))
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
            if (ObjectManager.Pet.HaveBuff("Mend Pet") && ObjectManager.GetUnitAttackPlayer().Count > 1 && settings.UseFreezingTrap)
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

        protected void Feed()
        {
            if (ObjectManager.Pet.IsAlive && !Me.IsCast && !ObjectManager.Pet.HaveBuff("Feed Pet Effect"))
            {
                _foodManager.FeedPet();
                Thread.Sleep(400);
            }
        }

        protected void PetManager()
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

        private void CombatDebug(string s)
        {
            if (settings.ActivateCombatDebug)
                Logger.CombatDebug(s);
        }

        private bool RaptorStrikeOn()
        {
            return Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Raptor Strike') then isAutoRepeat = true end", "isAutoRepeat");
        }

        private void ReenableAutoshot()
        {
            _autoshotRepeating = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(75); " +
                   "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");
            if (!_autoshotRepeating)
            {
                Logger.LogDebug("Re-enabling auto shot");
                AutoShot.Launch();
            }
        }

        protected Spell RevivePet = new Spell("Revive Pet");
        protected Spell CallPet = new Spell("Call Pet");
        protected Spell MendPet = new Spell("Mend Pet");
        protected Spell AspectHawk = new Spell("Aspect of the Hawk");
        protected Spell AspectCheetah = new Spell("Aspect of the Cheetah");
        protected Spell AspectMonkey = new Spell("Aspect of the Monkey");
        protected Spell AspectViper = new Spell("Aspect of the Viper");
        protected Spell HuntersMark = new Spell("Hunter's Mark");
        protected Spell ConcussiveShot = new Spell("Concussive Shot");
        protected Spell RaptorStrike = new Spell("Raptor Strike");
        protected Spell MongooseBite = new Spell("Mongoose Bite");
        protected Spell WingClip = new Spell("Wing Clip");
        protected Spell SerpentSting = new Spell("Serpent Sting");
        protected Spell ArcaneShot = new Spell("Arcane Shot");
        protected Spell AutoShot = new Spell("Auto Shot");
        protected Spell RapidFire = new Spell("Rapid Fire");
        protected Spell Intimidation = new Spell("Intimidation");
        protected Spell BestialWrath = new Spell("Bestial Wrath");
        protected Spell FeignDeath = new Spell("Feign Death");
        protected Spell FreezingTrap = new Spell("Freezing Trap");
        protected Spell SteadyShot = new Spell("Steady Shot");
        protected Spell KillCommand = new Spell("Kill Command");
        protected Spell Disengage = new Spell("Disengage");
        protected Spell Attack = new Spell("Attack");
        protected Spell BloodFury = new Spell("Blood Fury");
        protected Spell Berserking = new Spell("Berserking");
        protected Spell WarStomp = new Spell("War Stomp");
        protected Spell Stoneform = new Spell("Stoneform");
        protected Spell GiftOfTheNaaru = new Spell("Gift of the Naaru");
        protected Spell ManaTap = new Spell("Mana Tap");
        protected Spell ArcaneTorrent = new Spell("Arcane Torrent");
    }
}