using System;
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

namespace WholesomeTBCAIO.Rotations.Priest
{
    public class Priest : IClassRotation
    {
        public static PriestSettings settings;

        protected WoWLocalPlayer Me = ObjectManager.Me;
        protected Stopwatch _dispelTimer = new Stopwatch();

        protected readonly float _distaneRange = 26f;
        protected bool _usingWand = false;
        protected bool _iCanUseWand = ToolBox.HaveRangedWeaponEquipped();
        protected int _innerManaSaveThreshold = 20;
        protected int _wandThreshold;
        protected bool _goInMFRange = false;

        protected Priest specialization;

        public void Initialize(IClassRotation specialization)
        {
            Logger.Log("Initialized");
            settings = PriestSettings.Current;

            this.specialization = specialization as Priest;
            Talents.InitTalents(settings);

            _wandThreshold = settings.WandThreshold > 100 ? 50 : settings.WandThreshold;
            RangeManager.SetRange(_distaneRange);

            // Fight end
            FightEvents.OnFightEnd += (guid) =>
            {
                _usingWand = false;
                _goInMFRange = false;
                _dispelTimer.Reset();
                _iCanUseWand = false;
                RangeManager.SetRange(_distaneRange);
            };

            // Fight start
            FightEvents.OnFightStart += (unit, cancelable) =>
            {
                _iCanUseWand = ToolBox.HaveRangedWeaponEquipped();
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
                        if (!RangeManager.CurrentRangeIsMelee())
                        {
                            if (_goInMFRange)
                                RangeManager.SetRange(17f);
                            else
                                RangeManager.SetRange(_distaneRange);
                        }

                        if (!Fight.InFight)
                        {
                            specialization.BuffRotation();
                        }

                        if (Fight.InFight 
                            && ObjectManager.Me.Target > 0UL 
                            && ObjectManager.Target.IsAttackable 
                            && ObjectManager.Target.IsAlive)
                        {
                            if (ObjectManager.GetNumberAttackPlayer() < 1 && !ObjectManager.Target.InCombatFlagOnly)
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
                // OOC Cure Disease
                if (ToolBox.HasDiseaseDebuff())
                    if (Cast(CureDisease))
                        return;

                // OOC Renew
                if (Me.HealthPercent < 70 && !Me.HaveBuff("Renew"))
                    if (Cast(Renew))
                        return;

                // OOC Power Word Shield
                if (Me.HealthPercent < 50 && !Me.HaveBuff("Power Word: Shield") && !ToolBox.HasDebuff("Weakened Soul")
                    && ObjectManager.GetNumberAttackPlayer() > 0 && settings.UsePowerWordShield)
                    if (Cast(PowerWordShield))
                        return;

                // OOC Psychic Scream
                if (Me.HealthPercent < 30 && ObjectManager.GetNumberAttackPlayer() > 1)
                    if (Cast(PsychicScream))
                        return;

                // OOC Power Word Fortitude
                if (!Me.HaveBuff("Power Word: Fortitude") && PowerWordFortitude.KnownSpell && PowerWordFortitude.IsSpellUsable)
                {
                    Lua.RunMacroText("/target player");
                    if (Cast(PowerWordFortitude))
                    {
                        Lua.RunMacroText("/cleartarget");
                        return;
                    }
                }

                // OOC Divine Spirit
                if (!Me.HaveBuff("Divine Spirit") && DivineSpirit.KnownSpell && DivineSpirit.IsSpellUsable)
                {
                    Lua.RunMacroText("/target player");
                    if (Cast(DivineSpirit))
                    {
                        Lua.RunMacroText("/cleartarget");
                        return;
                    }
                }

                // OOC Inner Fire
                if (!Me.HaveBuff("Inner Fire") && settings.UseInnerFire)
                    if (Cast(InnerFire))
                        return;

                // OOC Shadowguard
                if (!Me.HaveBuff("Shadowguard") && settings.UseShadowGuard && Shadowguard.KnownSpell && Shadowguard.IsSpellUsable)
                {
                    Lua.RunMacroText("/target player");
                    if (Cast(Shadowguard))
                    {
                        Lua.RunMacroText("/cleartarget");
                        return;
                    }
                }

                // OOC Shadow Protection
                if (!Me.HaveBuff("Shadow Protection") && ShadowProtection.KnownSpell && settings.UseShadowProtection
                    && ShadowProtection.KnownSpell && ShadowProtection.IsSpellUsable)
                {
                    Lua.RunMacroText("/target player");
                    if (Cast(ShadowProtection))
                    {
                        Lua.RunMacroText("/cleartarget");
                        return;
                    }
                }

                // OOC ShadowForm
                if (!Me.HaveBuff("ShadowForm") && ObjectManager.GetNumberAttackPlayer() < 1 && Shadowform.IsSpellUsable)
                    if (Cast(Shadowform))
                        return;

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
            // Pull ShadowForm
            if (!Me.HaveBuff("ShadowForm"))
                if (Cast(Shadowform))
                    return;

            // Power Word Shield
            if (!ToolBox.HasDebuff("Weakened Soul") && settings.UseShieldOnPull
                && !Me.HaveBuff("Power Word: Shield") && settings.UsePowerWordShield)
                if (Cast(PowerWordShield))
                    return;

            // Vampiric Touch
            if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _distaneRange
                && !ObjectManager.Target.HaveBuff("Vampiric Touch"))
                if (Cast(VampiricTouch))
                    return;

            // MindBlast
            if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _distaneRange
                && !VampiricTouch.KnownSpell)
                if (Cast(MindBlast))
                    return;

            // Shadow Word Pain
            if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _distaneRange
                && (!MindBlast.KnownSpell || !MindBlast.IsSpellUsable)
                && !ObjectManager.Target.HaveBuff("Shadow Word: Pain"))
                if (Cast(ShadowWordPain))
                    return;

            // Holy Fire
            if (ObjectManager.Target.GetDistance <= _distaneRange && HolyFire.KnownSpell
                && HolyFire.IsSpellUsable && !Me.HaveBuff("ShadowForm"))
                if (Cast(HolyFire))
                    return;

            // Smite
            if (ObjectManager.Target.GetDistance <= _distaneRange && Smite.KnownSpell
                && !HolyFire.KnownSpell && Smite.IsSpellUsable && !Me.HaveBuff("ShadowForm"))
                if (Cast(Smite, false))
                    return;
        }

        protected virtual void CombatRotation()
        {
            _usingWand = Lua.LuaDoString<bool>("isAutoRepeat = false; local name = GetSpellInfo(5019); " +
                "if IsAutoRepeatSpell(name) then isAutoRepeat = true end", "isAutoRepeat");
            bool _hasMagicDebuff = ToolBox.HasMagicDebuff();
            bool _hasDisease = ToolBox.HasDiseaseDebuff();
            bool _hasWeakenedSoul = ToolBox.HasDebuff("Weakened Soul");
            double _myManaPC = Me.ManaPercentage;
            bool _inShadowForm = Me.HaveBuff("ShadowForm");
            int _mindBlastCD = Lua.LuaDoString<int>("local start, duration, enabled = GetSpellCooldown(\"Mind Blast\"); return start + duration - GetTime();");
            int _innerFocusCD = Lua.LuaDoString<int>("local start, duration, enabled = GetSpellCooldown(\"Inner Focus\"); return start + duration - GetTime();");
            bool _shoulBeInterrupted = ToolBox.EnemyCasting();
            WoWUnit Target = ObjectManager.Target;

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

            // Will of the Forsaken
            if (Me.HaveBuff("Fear") || Me.HaveBuff("Charm") || Me.HaveBuff("Sleep"))
                if (Cast(WillOfTheForsaken))
                    return;

            // Stoneform
            if (ToolBox.HasPoisonDebuff() || ToolBox.HasDiseaseDebuff() || Me.HaveBuff("Bleed"))
                if (Cast(Stoneform))
                    return;

            // Berserking
            if (Target.HealthPercent > 70)
                if (Cast(Berserking))
                    return;

            // Power Word Shield on multi aggro
            if (!Me.HaveBuff("Power Word: Shield") && !_hasWeakenedSoul
                && ObjectManager.GetNumberAttackPlayer() > 1 && settings.UsePowerWordShield)
                if (Cast(PowerWordShield))
                    return;

            // Power Word Shield
            if (Me.HealthPercent < 50 && !Me.HaveBuff("Power Word: Shield")
                && !_hasWeakenedSoul && settings.UsePowerWordShield)
                if (Cast(PowerWordShield))
                    return;

            // Renew
            if (Me.HealthPercent < 70 && !Me.HaveBuff("Renew") && !_inShadowForm
                 && (Target.HealthPercent > 15 || Me.HealthPercent < 25))
                if (Cast(Renew))
                    return;

            // Psychic Scream
            if (Me.HealthPercent < 50 && ObjectManager.GetNumberAttackPlayer() > 1)
                if (Cast(PsychicScream))
                    return;

            // Flash Heal
            if (Me.HealthPercent < 50 && (Target.HealthPercent > 15 || Me.HealthPercent < 25))
                if (Cast(FlashHeal))
                    return;

            // Heal
            if (Me.HealthPercent < 50 && (Target.HealthPercent > 15 || Me.HealthPercent < 25))
                if (Cast(Heal))
                    return;

            // Lesser Heal
            if (Me.HealthPercent < 50 && !FlashHeal.KnownSpell && (Target.HealthPercent > 15 || Me.HealthPercent < 25))
                if (Cast(LesserHeal))
                    return;

            // Silence
            if (_shoulBeInterrupted)
            {
                Thread.Sleep(Main.humanReflexTime);
                if (Cast(Silence))
                    return;
            }

            // Cure Disease
            if (_hasDisease && !_inShadowForm)
            {
                Thread.Sleep(Main.humanReflexTime);
                if (Cast(CureDisease))
                    return;
            }

            // Dispel Magic self
            if (_hasMagicDebuff && _myManaPC > 10 && DispelMagic.KnownSpell && DispelMagic.IsSpellUsable
                && (_dispelTimer.ElapsedMilliseconds > 10000 || _dispelTimer.ElapsedMilliseconds <= 0))
            {
                if (_usingWand)
                    ToolBox.StopWandWaitGCD(UseWand, Smite);
                Thread.Sleep(Main.humanReflexTime);
                Lua.RunMacroText("/target player");
                Lua.RunMacroText("/cast Dispel Magic");
                _dispelTimer.Restart();
                ToolBox.WaitGlobalCoolDown(Smite);
                return;
            }

            // Vampiric Touch
            if (Target.GetDistance <= _distaneRange && !Target.HaveBuff("Vampiric Touch")
                && _myManaPC > _innerManaSaveThreshold && Target.HealthPercent > _wandThreshold)
                if (Cast(VampiricTouch))
                    return;

            // Vampiric Embrace
            if (!Target.HaveBuff("Vampiric Embrace") && _myManaPC > _innerManaSaveThreshold)
                if (Cast(VampiricEmbrace))
                    return;

            // ShadowFiend
            if (ObjectManager.GetNumberAttackPlayer() > 1)
                if (Cast(Shadowfiend))
                    return;

            // Shadow Word Pain
            if (_myManaPC > 10 && Target.GetDistance < _distaneRange && Target.HealthPercent > 15
                && !Target.HaveBuff("Shadow Word: Pain"))
                if (Cast(ShadowWordPain))
                    return;

            // Inner Fire
            if (!Me.HaveBuff("Inner Fire") && settings.UseInnerFire && InnerFire.KnownSpell
                && _myManaPC > _innerManaSaveThreshold && Target.HealthPercent > _wandThreshold)
                if (Cast(InnerFire))
                    return;

            // Shadowguard
            if (!Me.HaveBuff("Shadowguard") && _myManaPC > _innerManaSaveThreshold
                && settings.UseShadowGuard && Target.HealthPercent > _wandThreshold)
                if (Cast(Shadowguard))
                    return;

            // Shadow Protection
            if (!Me.HaveBuff("Shadow Protection") && _myManaPC > 70 && settings.UseShadowProtection)
                if (Cast(ShadowProtection))
                    return;

            // Devouring Plague
            if (!Target.HaveBuff("Devouring Plague") && Target.HealthPercent > 80)
                if (Cast(DevouringPlague))
                    return;

            // Shadow Word Death
            if (_myManaPC > _innerManaSaveThreshold && Target.GetDistance < _distaneRange
            && settings.UseShadowWordDeath && Target.HealthPercent < 15)
                if (Cast(ShadowWordDeath))
                    return;

            // Mind Blast + Inner Focus
            if (!_inShadowForm && _myManaPC > _innerManaSaveThreshold && Target.GetDistance < _distaneRange
                && Target.HealthPercent > 50 && _mindBlastCD <= 0 && (Target.HealthPercent > _wandThreshold || !_iCanUseWand))
            {
                if (InnerFocus.KnownSpell && _innerFocusCD <= 0)
                    Cast(InnerFocus);

                if (Cast(MindBlast))
                    return;
            }

            // Shadow Form Mind Blast + Inner Focus
            if (_inShadowForm && _myManaPC > _innerManaSaveThreshold && Target.GetDistance < _distaneRange
                && _mindBlastCD <= 0 && Target.HealthPercent > _wandThreshold)
            {
                if (InnerFocus.KnownSpell && _innerFocusCD <= 0)
                    Cast(InnerFocus);

                if (Cast(MindBlast))
                    return;
            }

            // Mind Flay Range check
            if (_inShadowForm && !MindFlay.IsDistanceGood && (Me.HaveBuff("Power Word: Shield") || !settings.UsePowerWordShield))
            {
                Logger.LogDebug("Approaching to be in Mind Flay range");
                _goInMFRange = true;
                return;
            }

            // Mind FLay
            if ((Me.HaveBuff("Power Word: Shield") || !settings.UsePowerWordShield) && MindFlay.IsDistanceGood
                && _myManaPC > _innerManaSaveThreshold && Target.HealthPercent > _wandThreshold)
                if (Cast(MindFlay, false))
                    return;

            // Low level Smite
            if (Me.Level < 5 && (Target.HealthPercent > 30 || Me.ManaPercentage > 80) && _myManaPC > _innerManaSaveThreshold
                && Target.GetDistance < _distaneRange)
                if (Cast(Smite, false))
                    return;

            // Smite
            if (!_inShadowForm && _myManaPC > _innerManaSaveThreshold && Target.GetDistance < _distaneRange
                && Me.Level >= 5 && Target.HealthPercent > 20 && (Target.HealthPercent > settings.WandThreshold || !_iCanUseWand))
                if (Cast(Smite, false))
                    return;

            // Use Wand
            if (!_usingWand && _iCanUseWand && Target.GetDistance <= _distaneRange + 2)
            {
                RangeManager.SetRange(_distaneRange);
                if (Cast(UseWand, false))
                    return;
            }

            // Go in melee because nothing else to do
            if (!_usingWand
                && !_iCanUseWand
                && !RangeManager.CurrentRangeIsMelee()
                && Target.IsAlive)
            {
                Logger.Log("Going in melee");
                RangeManager.SetRangeToMelee();
                return;
            }
        }

        protected Spell Smite = new Spell("Smite");
        protected Spell LesserHeal = new Spell("Lesser Heal");
        protected Spell PowerWordFortitude = new Spell("Power Word: Fortitude");
        protected Spell PowerWordShield = new Spell("Power Word: Shield");
        protected Spell ShadowWordPain = new Spell("Shadow Word: Pain");
        protected Spell ShadowWordDeath = new Spell("Shadow Word: Death");
        protected Spell UseWand = new Spell("Shoot");
        protected Spell Renew = new Spell("Renew");
        protected Spell MindBlast = new Spell("Mind Blast");
        protected Spell InnerFire = new Spell("Inner Fire");
        protected Spell CureDisease = new Spell("Cure Disease");
        protected Spell PsychicScream = new Spell("Psychic Scream");
        protected Spell Heal = new Spell("Heal");
        protected Spell MindFlay = new Spell("Mind Flay");
        protected Spell HolyFire = new Spell("Holy Fire");
        protected Spell DispelMagic = new Spell("Dispel Magic");
        protected Spell FlashHeal = new Spell("Flash Heal");
        protected Spell VampiricEmbrace = new Spell("Vampiric Embrace");
        protected Spell Shadowguard = new Spell("Shadowguard");
        protected Spell ShadowProtection = new Spell("Shadow Protection");
        protected Spell Shadowform = new Spell("Shadowform");
        protected Spell VampiricTouch = new Spell("Vampiric Touch");
        protected Spell InnerFocus = new Spell("Inner Focus");
        protected Spell Shadowfiend = new Spell("Shadowfiend");
        protected Spell Silence = new Spell("Silence");
        protected Spell DivineSpirit = new Spell("Divine Spirit");
        protected Spell DevouringPlague = new Spell("Devouring Plague");
        protected Spell Cannibalize = new Spell("Cannibalize");
        protected Spell WillOfTheForsaken = new Spell("Will of the Forsaken");
        protected Spell Berserking = new Spell("Berserking");
        protected Spell Stoneform = new Spell("Stoneform");
        protected Spell GiftOfTheNaaru = new Spell("Gift of the Naaru");
        protected Spell ManaTap = new Spell("Mana Tap");
        protected Spell ArcaneTorrent = new Spell("Arcane Torrent");

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
                ToolBox.StopWandWaitGCD(UseWand, Smite);

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