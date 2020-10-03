using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using WholesomeTBCAIO.Helpers;
using WholesomeTBCAIO.Settings;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeTBCAIO.Rotations.Warlock
{
    public class Warlock : IClassRotation
    {
        public static WarlockSettings settings;

        protected BackgroundWorker _petPulseThread = new BackgroundWorker();
        protected Stopwatch _addCheckTimer = new Stopwatch();
        protected WoWLocalPlayer Me = ObjectManager.Me;

        protected float _maxRange = 27f;
        protected bool _usingWand = false;
        protected int _innerManaSaveThreshold = 20;
        protected bool _iCanUseWand = ToolBox.HaveRangedWeaponEquipped();
        protected int _saveDrinkPercent = wManager.wManagerSetting.CurrentSetting.DrinkPercent;

        protected Warlock specialization;

        public void Initialize(IClassRotation specialization)
        {
            Logger.Log("Initialized");
            settings = WarlockSettings.Current;

            this.specialization = specialization as Warlock;
            Talents.InitTalents(settings);

            _petPulseThread.DoWork += PetThread;
            _petPulseThread.RunWorkerAsync();

            RangeManager.SetRange(_maxRange);

            // Set pet mode
            if (settings.PetInPassiveWhenOOC)
                Lua.LuaDoString("PetPassiveMode();");
            else
                Lua.LuaDoString("PetDefensiveMode();");

            // Fight end
            FightEvents.OnFightEnd += (guid) =>
            {
                _usingWand = false;
                _iCanUseWand = false;
                RangeManager.SetRange(_maxRange);
                _addCheckTimer.Reset();
                if (settings.PetInPassiveWhenOOC)
                    Lua.LuaDoString("PetPassiveMode();");
            };

            // Fight start
            FightEvents.OnFightStart += (unit, cancelable) =>
            {
                if (UseWand.IsSpellUsable)
                    _iCanUseWand = true;
                Lua.LuaDoString("PetDefensiveMode();");

                // Imp Firebolt
                if (WarlockPetAndConsumables.MyWarlockPet().Equals("Imp"))
                    ToolBox.TogglePetSpellAuto("Firebolt", true);

                // Imp BloodPact
                if (WarlockPetAndConsumables.MyWarlockPet().Equals("Imp"))
                    ToolBox.TogglePetSpellAuto("Blood Pact", true);
            };

            Rotation();
        }

        public void Dispose()
        {
            Logger.Log("Stop in progress.");
            _petPulseThread.DoWork -= PetThread;
            _petPulseThread.Dispose();
            Lua.LuaDoString("PetPassiveMode();");
            wManager.wManagerSetting.CurrentSetting.DrinkPercent = _saveDrinkPercent;
        }

        // Pet thread
        protected void PetThread(object sender, DoWorkEventArgs args)
        {
            while (Main.isLaunched)
            {
                try
                {
                    if (Conditions.InGameAndConnectedAndProductStartedNotInPause 
                        && !ObjectManager.Me.IsOnTaxi 
                        && ObjectManager.Me.IsAlive
                        && ObjectManager.Pet.IsValid 
                        && !Main.HMPrunningAway)
                    {
                        // Voidwalker Torment
                        if (WarlockPetAndConsumables.MyWarlockPet().Equals("Voidwalker")
                            && ObjectManager.Target.Target == Me.Guid
                            && Me.InCombatFlagOnly
                            && !settings.AutoTorment)
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
                        if (!Me.InCombatFlagOnly)
                        {
                            specialization.BuffRotation();
                        }

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
            if (!Me.IsMounted)
            {
                // Make sure we have mana to summon
                if (!ObjectManager.Pet.IsValid
                    && ObjectManager.Me.ManaPercentage < 95
                    && !ObjectManager.Me.HaveBuff("Drink")
                    && (SummonVoidwalker.KnownSpell && !SummonVoidwalker.IsSpellUsable && ToolBox.CountItemStacks("Soul Shard") > 0 ||
                    SummonImp.KnownSpell && !SummonImp.IsSpellUsable && !SummonVoidwalker.KnownSpell))
                {
                    Logger.Log("Not enough mana to summon, forcing regen");
                    wManager.wManagerSetting.CurrentSetting.DrinkPercent = 95;
                    Thread.Sleep(1000);
                    return;
                }
                else
                    wManager.wManagerSetting.CurrentSetting.DrinkPercent = _saveDrinkPercent;

                // Switch Auto Torment & Suffering off
                if (WarlockPetAndConsumables.MyWarlockPet().Equals("Voidwalker"))
                {
                    ToolBox.TogglePetSpellAuto("Torment", settings.AutoTorment);
                    ToolBox.TogglePetSpellAuto("Suffering", false);
                }

                // Summon Felguard
                if ((!ObjectManager.Pet.IsValid
                    || WarlockPetAndConsumables.MyWarlockPet().Equals("Voidwalker") || WarlockPetAndConsumables.MyWarlockPet().Equals("Imp"))
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
                if (SummonFelguard.KnownSpell
                    && (ObjectManager.Pet.ManaPercentage < 20 || ObjectManager.Pet.HealthPercent < 20)
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
                if ((!ObjectManager.Pet.IsValid || !WarlockPetAndConsumables.MyWarlockPet().Equals("Voidwalker"))
                    && SummonVoidwalker.KnownSpell
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
                if (WarlockPetAndConsumables.MyWarlockPet().Equals("Voidwalker")
                    && SummonVoidwalker.KnownSpell
                    && ObjectManager.Pet.ManaPercentage < 20
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
                if (!ObjectManager.Pet.IsValid && SummonImp.KnownSpell
                    && (!SummonVoidwalker.KnownSpell || ToolBox.CountItemStacks("Soul Shard") < 1))
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
                if (Me.HealthPercent > Me.ManaPercentage
                    && settings.UseLifeTap)
                    if (Cast(LifeTap))
                        return;

                // Unending Breath
                if (!Me.HaveBuff("Unending Breath")
                    && UnendingBreath.KnownSpell
                    && UnendingBreath.IsSpellUsable
                    && settings.UseUnendingBreath)
                {
                    Lua.RunMacroText("/target player");
                    if (Cast(UnendingBreath))
                    {
                        Lua.RunMacroText("/cleartarget");
                        return;
                    }
                }

                // Demon Skin
                if (!Me.HaveBuff("Demon Skin")
                    && !DemonArmor.KnownSpell
                    && DemonSkin.KnownSpell)
                    if (Cast(DemonSkin))
                        return;

                // Demon Armor
                if ((!Me.HaveBuff("Demon Armor") || Me.HaveBuff("Demon Skin"))
                    && DemonArmor.KnownSpell
                    && (!FelArmor.KnownSpell || FelArmor.KnownSpell && !settings.UseFelArmor))
                    if (Cast(DemonArmor))
                        return;

                // Soul Link
                if (SoulLink.KnownSpell
                    && !Me.HaveBuff("Soul Link"))
                    if (Cast(SoulLink))
                        return;

                // Fel Armor
                if (!Me.HaveBuff("Fel Armor")
                    && FelArmor.KnownSpell
                    && settings.UseFelArmor)
                    if (Cast(FelArmor))
                        return;

                // Health Funnel
                if (ObjectManager.Pet.HealthPercent < 50
                    && Me.HealthPercent > 40
                    && ObjectManager.Pet.GetDistance < 19
                    && !ObjectManager.Pet.InCombatFlagOnly
                    && HealthFunnel.KnownSpell)
                {
                    Fight.StopFight();
                    MovementManager.StopMove();
                    if (WarlockPetAndConsumables.MyWarlockPet().Equals("Voidwalker")
                        && ToolBox.GetPetSpellIndex("Consume Shadows") != 0)
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
                if (!WarlockPetAndConsumables.HaveHealthstone())
                    if (Cast(CreateHealthStone))
                        return;

                // Create Soul Stone
                if (!WarlockPetAndConsumables.HaveSoulstone()
                    && CreateSoulstone.KnownSpell)
                    if (Cast(CreateSoulstone))
                        return;

                // Use Soul Stone
                if (!Me.HaveBuff("Soulstone Resurrection")
                    && CreateSoulstone.KnownSpell
                    && ToolBox.HaveOneInList(WarlockPetAndConsumables.SoulStones())
                    && ToolBox.GetItemCooldown(WarlockPetAndConsumables.SoulStones()) <= 0)
                {
                    MovementManager.StopMove();
                    Lua.RunMacroText("/target player");
                    WarlockPetAndConsumables.UseSoulstone();
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

        protected virtual void Pull()
        {
            // Pet attack
            if (ObjectManager.Pet.Target != ObjectManager.Me.Target)
                Lua.LuaDoString("PetAttack();", false);

            // Life Tap
            if (Me.HealthPercent > Me.ManaPercentage
                && settings.UseLifeTap)
                if (Cast(LifeTap))
                    return;

            // Amplify Curse
            if (AmplifyCurse.IsSpellUsable
                && AmplifyCurse.KnownSpell)
                AmplifyCurse.Launch();

            // Siphon Life
            if (Me.HealthPercent < 90
                && settings.UseSiphonLife
                && !ObjectManager.Target.HaveBuff("Siphon Life")
                && ObjectManager.Target.GetDistance < _maxRange + 2)
                if (Cast(SiphonLife))
                    return;

            // Unstable Affliction
            if (ObjectManager.Target.GetDistance < _maxRange + 2
                && !ObjectManager.Target.HaveBuff("Unstable Affliction"))
                if (Cast(UnstableAffliction))
                    return;

            // Curse of Agony
            if (ObjectManager.Target.GetDistance < _maxRange + 2
                && !ObjectManager.Target.HaveBuff("Curse of Agony"))
                if (Cast(CurseOfAgony))
                    return;

            // Corruption
            if (ObjectManager.Target.GetDistance < _maxRange + 2
                && !ObjectManager.Target.HaveBuff("Corruption"))
                if (Cast(Corruption))
                    return;

            // Immolate
            if (ObjectManager.Target.GetDistance < _maxRange + 2
                && !ObjectManager.Target.HaveBuff("Immolate")
                && !Corruption.KnownSpell
                && ToolBox.CanBleed(ObjectManager.Target))
                if (Cast(Immolate))
                    return;

            // Shadow Bolt
            if (ObjectManager.Target.GetDistance < _maxRange + 2
                && !Immolate.KnownSpell)
                if (Cast(ShadowBolt))
                    return;
        }

        protected virtual void CombatRotation()
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
            if (Me.HaveBuff("Mana Tap") && Me.ManaPercentage < 50
                || Target.IsCast && Target.GetDistance < 8)
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
            if (ToolBox.CountItemStacks("Soul Shard") < settings.NumberOfSoulShards
                && Target.HealthPercent < 40)
                if (Cast(DrainSoul))
                    return;

            // Use Health Stone
            if (Me.HealthPercent < 15)
                WarlockPetAndConsumables.UseHealthstone();

            // Shadow Trance
            if (Me.HaveBuff("Shadow Trance") && _overLowManaThreshold)
                if (Cast(ShadowBolt))
                    return;

            // Siphon Life
            if (Me.HealthPercent < 90
                && _overLowManaThreshold
                && Target.HealthPercent > 20
                && !Target.HaveBuff("Siphon Life")
                && settings.UseSiphonLife)
                if (Cast(SiphonLife))
                    return;

            // Death Coil
            if (Me.HealthPercent < 20)
                if (Cast(DeathCoil))
                    return;

            // Drain Life low
            if (Me.HealthPercent < 30
                && Target.HealthPercent > 20)
                if (Cast(DrainLife))
                    return;

            // Curse of Agony
            if (ObjectManager.Target.GetDistance < _maxRange
                && !Target.HaveBuff("Curse of Agony")
                && _overLowManaThreshold
                && Target.HealthPercent > 20)
                if (Cast(CurseOfAgony))
                    return;

            // Unstable Affliction
            if (ObjectManager.Target.GetDistance < _maxRange
                && !Target.HaveBuff("Unstable Affliction")
                && _overLowManaThreshold
                && Target.HealthPercent > 30)
                if (Cast(UnstableAffliction))
                    return;

            // Corruption
            if (ObjectManager.Target.GetDistance < _maxRange
                && !Target.HaveBuff("Corruption")
                && _overLowManaThreshold
                && Target.HealthPercent > 20)
                if (Cast(Corruption))
                    return;

            // Immolate
            if (ObjectManager.Target.GetDistance < _maxRange
                && !Target.HaveBuff("Immolate")
                && _overLowManaThreshold
                && Target.HealthPercent > 30
                && (settings.UseImmolateHighLevel || !UnstableAffliction.KnownSpell)
                && ToolBox.CanBleed(ObjectManager.Target))
                if (Cast(Immolate))
                    return;

            // Drain Life high
            if (Me.HealthPercent < 70
                && Target.HealthPercent > 20)
                if (Cast(DrainLife))
                    return;

            // Health Funnel
            if (ObjectManager.Pet.IsValid
                && ObjectManager.Pet.HealthPercent < 30
                && Me.HealthPercent > 30)
            {
                RangeManager.SetRange(19f);
                if (HealthFunnel.IsDistanceGood && Cast(HealthFunnel))
                    return;
            }

            // Dark Pact
            if (Me.ManaPercentage < 70
                && ObjectManager.Pet.Mana > 0
                && ObjectManager.Pet.ManaPercentage > 60
                && settings.UseDarkPact)
                if (Cast(DarkPact))
                    return;

            // Drain Mana
            if (Me.ManaPercentage < 70
                && Target.Mana > 0
                && Target.ManaPercentage > 30)
                if (Cast(DrainMana))
                    return;

            // Incinerate
            if (ObjectManager.Target.GetDistance < _maxRange && Target.HaveBuff("Immolate")
                && _overLowManaThreshold
                && Target.HealthPercent > 30
                && settings.UseIncinerate)
                if (Cast(Incinerate))
                    return;

            // Shadow Bolt
            if ((!settings.PrioritizeWandingOverSB || !_iCanUseWand)
                && (ObjectManager.Target.HealthPercent > 50 || Me.ManaPercentage > 90 && ObjectManager.Target.HealthPercent > 10)
                && _myManaPC > 40
                && ObjectManager.Target.GetDistance < _maxRange)
                if (Cast(ShadowBolt))
                    return;

            // Life Tap
            if (Me.HealthPercent > 50
                && Me.ManaPercentage < 40
                && !ObjectManager.Target.IsTargetingMe
                && settings.UseLifeTap)
                if (Cast(LifeTap))
                    return;

            // Use Wand
            if (!_usingWand
                && _iCanUseWand
                && ObjectManager.Target.GetDistance <= _maxRange + 2)
            {
                RangeManager.SetRange(_maxRange);
                if (Cast(UseWand, false))
                    return;
            }

            // Go in melee because nothing else to do
            if (!_usingWand && !UseWand.IsSpellUsable
                && !RangeManager.CurrentRangeIsMelee()
                && Target.IsAlive)
            {
                Logger.Log("Going in melee");
                RangeManager.SetRangeToMelee();
                return;
            }
        }

        protected Spell DemonSkin = new Spell("Demon Skin");
        protected Spell DemonArmor = new Spell("Demon Armor");
        protected Spell ShadowBolt = new Spell("Shadow Bolt");
        protected Spell UseWand = new Spell("Shoot");
        protected Spell Fear = new Spell("Fear");
        protected Spell Immolate = new Spell("Immolate");
        protected Spell Corruption = new Spell("Corruption");
        protected Spell LifeTap = new Spell("Life Tap");
        protected Spell SummonImp = new Spell("Summon Imp");
        protected Spell SummonVoidwalker = new Spell("Summon Voidwalker");
        protected Spell SummonFelguard = new Spell("Summon Felguard");
        protected Spell CurseOfAgony = new Spell("Curse of Agony");
        protected Spell DrainSoul = new Spell("Drain Soul");
        protected Spell DrainLife = new Spell("Drain Life");
        protected Spell CreateHealthStone = new Spell("Create HealthStone");
        protected Spell HealthFunnel = new Spell("Health Funnel");
        protected Spell CreateSoulstone = new Spell("Create Soulstone");
        protected Spell AmplifyCurse = new Spell("Amplify Curse");
        protected Spell UnendingBreath = new Spell("Unending Breath");
        protected Spell SiphonLife = new Spell("Siphon Life");
        protected Spell DrainMana = new Spell("Drain Mana");
        protected Spell DarkPact = new Spell("Dark Pact");
        protected Spell UnstableAffliction = new Spell("Unstable Affliction");
        protected Spell DeathCoil = new Spell("Death Coil");
        protected Spell FelArmor = new Spell("Fel Armor");
        protected Spell Incinerate = new Spell("Incinerate");
        protected Spell SoulShatter = new Spell("Soulshatter");
        protected Spell FelDomination = new Spell("Fel Domination");
        protected Spell Cannibalize = new Spell("Cannibalize");
        protected Spell WillOfTheForsaken = new Spell("Will of the Forsaken");
        protected Spell Berserking = new Spell("Berserking");
        protected Spell EscapeArtist = new Spell("Escape Artist");
        protected Spell ManaTap = new Spell("Mana Tap");
        protected Spell ArcaneTorrent = new Spell("Arcane Torrent");
        protected Spell SoulLink = new Spell("Soul Link");

        protected bool Cast(Spell s, bool castEvenIfWanding = true)
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
            if (ObjectManager.Target.IsAlive || !Fight.InFight && ObjectManager.Target.Guid < 1)
            {
                s.Launch();
                Usefuls.WaitIsCasting();
            }
            return true;
        }

        protected void CombatDebug(string s)
        {
            if (settings.ActivateCombatDebug)
                Logger.CombatDebug(s);
        }
    }
}