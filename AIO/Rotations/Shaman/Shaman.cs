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

namespace WholesomeTBCAIO.Rotations.Shaman
{
    public class Shaman : IClassRotation
    {
        public static ShamanSettings settings;

        protected TotemManager totemManager = new TotemManager();

        protected Stopwatch _ghostWolfTimer = new Stopwatch();
        protected Stopwatch _pullMeleeTimer = new Stopwatch();
        protected Stopwatch _meleeTimer = new Stopwatch();
        protected WoWLocalPlayer Me = ObjectManager.Me;

        protected bool _fightingACaster = false;
        protected float _pullRange = 28f;
        protected int _lowManaThreshold = 20;
        protected int _mediumManaThreshold = 50;
        protected List<string> _casterEnemies = new List<string>();
        protected int _pullAttempt;

        protected Shaman specialization;

        public void Initialize(IClassRotation specialization)
        {
            Logger.Log("Initialized");
            settings = ShamanSettings.Current;

            this.specialization = specialization as Shaman;
            Talents.InitTalents(settings);

            _ghostWolfTimer.Start();
            RangeManager.SetRange(_pullRange);

            FightEvents.OnFightEnd += (guid) =>
            {
                _ghostWolfTimer.Restart();
                _fightingACaster = false;
                _meleeTimer.Reset();
                _pullMeleeTimer.Reset();
                _pullAttempt = 0;
            };

            FightEvents.OnFightStart += (unit, cancelable) =>
            {
                _ghostWolfTimer.Reset();
                RangeManager.SetRange(_pullRange);
            };

            robotManager.Events.FiniteStateMachineEvents.OnRunState += (engine, state, cancelable) =>
            {
                if (state.DisplayName == "Regeneration")
                    _ghostWolfTimer.Reset();
            };

            robotManager.Events.FiniteStateMachineEvents.OnAfterRunState += (engine, state) =>
            {
                if (state.DisplayName == "Regeneration")
                    _ghostWolfTimer.Restart();
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
                        ApplyEnchantWeapon();
                        totemManager.CheckForTotemicCall();

                        // Ghost Wolf
                        if (Me.ManaPercentage > 50
                            && !Me.IsIndoors
                            && _ghostWolfTimer.ElapsedMilliseconds > 3000
                            && settings.UseGhostWolf
                            && !Me.IsMounted
                            && !Fight.InFight
                            && !Me.HaveBuff("Ghost Wolf")
                            && !ObjectManager.Target.IsFlightMaster)
                        {
                            _ghostWolfTimer.Stop();
                            Cast(GhostWolf);
                        }

                        // Buff rotation
                        if (ObjectManager.GetNumberAttackPlayer() < 1
                            && !Me.InCombatFlagOnly)
                            specialization.BuffRotation();

                        // Pull & Combat rotation
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
            if (!Me.IsMounted && !Me.HaveBuff("Ghost Wolf") && !Me.IsCast)
            {
                // Lesser Healing Wave OOC
                if (Me.HealthPercent < settings.OOCHealThreshold)
                    if (Cast(LesserHealingWave))
                        return;

                // Healing Wave OOC
                if (Me.HealthPercent < settings.OOCHealThreshold)
                    if (Cast(HealingWave))
                        return;

                // Water Shield
                if (!Me.HaveBuff("Water Shield")
                    && !Me.HaveBuff("Lightning Shield")
                    && (settings.UseWaterShield || !settings.UseLightningShield || Me.ManaPercentage < 20))
                    if (Cast(WaterShield))
                        return;
            }
        }

        protected virtual void Pull()
        {
            // Check if caster
            if (_casterEnemies.Contains(ObjectManager.Target.Name))
                _fightingACaster = true;

            // Water Shield
            if (!Me.HaveBuff("Water Shield")
                && !Me.HaveBuff("Lightning Shield")
                && (settings.UseWaterShield || !settings.UseLightningShield) || Me.ManaPercentage < _lowManaThreshold)
                if (Cast(WaterShield))
                    return;

            // Ligntning Shield
            if (Me.ManaPercentage > _lowManaThreshold
                && !Me.HaveBuff("Lightning Shield")
                && !Me.HaveBuff("Water Shield")
                && settings.UseLightningShield
                && (!WaterShield.KnownSpell || !settings.UseWaterShield))
                if (Cast(LightningShield))
                    return;
        }

        protected virtual void CombatRotation()
        {
            WoWUnit Target = ObjectManager.Target;
            bool _isPoisoned = ToolBox.HasPoisonDebuff();
            bool _hasDisease = ToolBox.HasDiseaseDebuff();

            // Gift of the Naaru
            if (ObjectManager.GetNumberAttackPlayer() > 1
                && Me.HealthPercent < 50)
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

            // Warstomp
            if (ObjectManager.GetNumberAttackPlayer() > 1
                && Target.GetDistance < 8)
                if (Cast(WarStomp))
                    return;

            // Healing Wave + Lesser Healing Wave
            if (Me.HealthPercent < settings.HealThreshold
                && (Target.HealthPercent > 15 || Me.HealthPercent < 25))
                if (Cast(LesserHealingWave) || Cast(HealingWave))
                    return;

            // Cure Poison
            if (settings.CurePoison
                && _isPoisoned 
                && Me.ManaPercentage > _lowManaThreshold)
            {
                Thread.Sleep(Main.humanReflexTime);
                if (Cast(CurePoison))
                    return;
            }

            // Cure Disease
            if (settings.CureDisease
                && _hasDisease 
                && Me.ManaPercentage > _lowManaThreshold)
            {
                Thread.Sleep(Main.humanReflexTime);
                if (Cast(CureDisease))
                    return;
            }

            // Lightning Shield
            if (Me.ManaPercentage > _lowManaThreshold
                && !Me.HaveBuff("Lightning Shield")
                && !Me.HaveBuff("Water Shield")
                && settings.UseLightningShield
                && (!WaterShield.KnownSpell || !settings.UseWaterShield))
                if (Cast(LightningShield))
                    return;
        }

        protected Spell LightningBolt = new Spell("Lightning Bolt");
        protected Spell HealingWave = new Spell("Healing Wave");
        protected Spell LesserHealingWave = new Spell("Lesser Healing Wave");
        protected Spell RockbiterWeapon = new Spell("Rockbiter Weapon");
        protected Spell EarthShock = new Spell("Earth Shock");
        protected Spell FlameShock = new Spell("Flame Shock");
        protected Spell FrostShock = new Spell("Frost Shock");
        protected Spell LightningShield = new Spell("Lightning Shield");
        protected Spell WaterShield = new Spell("Water Shield");
        protected Spell GhostWolf = new Spell("Ghost Wolf");
        protected Spell CurePoison = new Spell("Cure Poison");
        protected Spell CureDisease = new Spell("Cure Disease");
        protected Spell WindfuryWeapon = new Spell("Windfury Weapon");
        protected Spell Stormstrike = new Spell("Stormstrike");
        protected Spell ShamanisticRage = new Spell("Shamanistic Rage");
        protected Spell Attack = new Spell("Attack");
        protected Spell BloodFury = new Spell("Blood Fury");
        protected Spell Berserking = new Spell("Berserking");
        protected Spell WarStomp = new Spell("War Stomp");
        protected Spell GiftOfTheNaaru = new Spell("Gift of the Naaru");
        protected Spell ElementalMastery = new Spell("Elemental Mastery");
        protected Spell ChainLightning = new Spell("Chain Lightning");

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

                int limit = 0;
                while (ToolBox.GetSpellCooldown(s.Name) > 0)
                {
                    Thread.Sleep(50);
                    limit += 50;
                    if (limit > 2000)
                    {
                        CombatDebug(s.Name + ": waited for tool long, give up");
                        return false;
                    }
                }
                Thread.Sleep(ToolBox.GetLatency());
                CombatDebug(s.Name + ": waited " + (limit) + " for it to be ready");
            }

            if (!s.IsSpellUsable)
            {
                CombatDebug("Didn't cast because spell somehow not usable");
                return false;
            }

            CombatDebug("Launching");
            if (ObjectManager.Target.IsAlive || !Fight.InFight && ObjectManager.Target.Guid < 1)
            {
                s.Launch(true);
            }
            return true;
        }

        protected void CombatDebug(string s)
        {
            if (settings.ActivateCombatDebug)
                Logger.CombatDebug(s);
        }

        protected virtual void ApplyEnchantWeapon()
        {
            if (!HaveMainHandEnchant || HaveOffHandWheapon && !HaveOffHandEnchant)
            {
                if (!WindfuryWeapon.KnownSpell && RockbiterWeapon.KnownSpell)
                    Cast(RockbiterWeapon);

                if (WindfuryWeapon.KnownSpell)
                    Cast(WindfuryWeapon);
            }
        }

        protected bool HaveMainHandEnchant => Lua.LuaDoString<bool>
                (@"local hasMainHandEnchant, _, _, _, _, _, _, _, _ = GetWeaponEnchantInfo()
                    if (hasMainHandEnchant) then 
                       return '1'
                    else
                       return '0'
                    end");

        protected bool HaveOffHandEnchant => Lua.LuaDoString<bool>
                (@"local _, _, _, _, hasOffHandEnchant, _, _, _, _ = GetWeaponEnchantInfo()
                    if (hasOffHandEnchant) then 
                       return '1'
                    else
                       return '0'
                    end");

        protected bool HaveOffHandWheapon => Lua.LuaDoString<bool>(@"local hasWeapon = OffhandHasWeapon()
                return hasWeapon");
    }
}