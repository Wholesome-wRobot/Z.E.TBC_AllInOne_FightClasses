using System;
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

public static class Priest
{
    private static WoWLocalPlayer Me = ObjectManager.Me;
    private static float _maxRange = 28f;
    private static float _meleeRange = 5f;
    private static bool _usingWand = false;
    private static bool _iCanUseWand = ToolBox.HaveRangedWeaponEquipped();
    private static int _innerManaSaveThreshold = 20;
    private static int _wandThreshold;
    private static bool _goInMFRange = false;
    private static Stopwatch _dispelTimer = new Stopwatch();
    private static ZEPriestSettings _settings;

    public static void Initialize()
    {
        Main.Log("Initialized");
        ZEPriestSettings.Load();
        _settings = ZEPriestSettings.CurrentSetting;
        Talents.InitTalents(_settings.AssignTalents, _settings.UseDefaultTalents, _settings.TalentCodes);
        _wandThreshold = _settings.WandThreshold > 100 ? 50 : _settings.WandThreshold;
        Main.settingRange = _maxRange;

        // Fight end
        FightEvents.OnFightEnd += (ulong guid) =>
        {
            _usingWand = false;
            _goInMFRange = false;
            _dispelTimer.Reset();
            _iCanUseWand = false;
            Main.settingRange = _maxRange;
        };

        // Fight start
        FightEvents.OnFightStart += (WoWUnit unit, CancelEventArgs cancelable) =>
        {
            _iCanUseWand = ToolBox.HaveRangedWeaponEquipped();
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
                    if (Main.settingRange != _meleeRange)
                        Main.settingRange = _goInMFRange ? 17f : _maxRange - 2;
                    if (!Fight.InFight)
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
                && ObjectManager.GetNumberAttackPlayer() > 0 && _settings.UsePowerWordShield)
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
            if (!Me.HaveBuff("Inner Fire") && _settings.UseInnerFire)
                if (Cast(InnerFire))
                    return;

            // OOC Shadowguard
            if (!Me.HaveBuff("Shadowguard") && _settings.UseShadowGuard && Shadowguard.KnownSpell && Shadowguard.IsSpellUsable)
            {
                Lua.RunMacroText("/target player");
                if (Cast(Shadowguard))
                {
                    Lua.RunMacroText("/cleartarget");
                    return;
                }
            }

            // OOC Shadow Protection
            if (!Me.HaveBuff("Shadow Protection") && ShadowProtection.KnownSpell && _settings.UseShadowProtection
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

    internal static void Pull()
    {
        // Pull ShadowForm
        if (!Me.HaveBuff("ShadowForm"))
            if (Cast(Shadowform))
                return;

        // Power Word Shield
        if (!ToolBox.HasDebuff("Weakened Soul") && _settings.UseShieldOnPull
            && !Me.HaveBuff("Power Word: Shield") && _settings.UsePowerWordShield)
            if (Cast(PowerWordShield))
                return;

        // Vampiric Touch
        if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _maxRange 
            && !ObjectManager.Target.HaveBuff("Vampiric Touch"))
            if (Cast(VampiricTouch))
                return;

        // MindBlast
        if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _maxRange
            && !VampiricTouch.KnownSpell)
            if (Cast(MindBlast))
                return;

        // Shadow Word Pain
        if (Me.HaveBuff("ShadowForm") && ObjectManager.Target.GetDistance <= _maxRange 
            && (!MindBlast.KnownSpell || !MindBlast.IsSpellUsable)
            && !ObjectManager.Target.HaveBuff("Shadow Word: Pain"))
            if (Cast(ShadowWordPain))
                return;

        // Holy Fire
        if (ObjectManager.Target.GetDistance <= _maxRange && HolyFire.KnownSpell 
            && HolyFire.IsSpellUsable && !Me.HaveBuff("ShadowForm"))
            if (Cast(HolyFire))
                return;

        // Smite
        if (ObjectManager.Target.GetDistance <= _maxRange && Smite.KnownSpell 
            && !HolyFire.KnownSpell && Smite.IsSpellUsable && !Me.HaveBuff("ShadowForm"))
            if (Cast(Smite, false))
                return;
    }

    internal static void CombatRotation()
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
        if ((Me.HaveBuff("Mana Tap") && Me.ManaPercentage < 50)
            || (Target.IsCast && Target.GetDistance < 8))
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
            && ObjectManager.GetNumberAttackPlayer() > 1 && _settings.UsePowerWordShield)
            if (Cast(PowerWordShield))
                return;

        // Power Word Shield
        if (Me.HealthPercent < 50 && !Me.HaveBuff("Power Word: Shield") 
            && !_hasWeakenedSoul && _settings.UsePowerWordShield)
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
            Thread.Sleep(Main._humanReflexTime);
            if (Cast(Silence))
                return;
        }

        // Cure Disease
        if (_hasDisease && !_inShadowForm)
        {
            Thread.Sleep(Main._humanReflexTime);
            if (Cast(CureDisease))
                return;
        }

        // Dispel Magic self
        if (_hasMagicDebuff && _myManaPC > 10 && DispelMagic.KnownSpell && DispelMagic.IsSpellUsable
            && (_dispelTimer.ElapsedMilliseconds > 10000 || _dispelTimer.ElapsedMilliseconds <= 0))
        {
            if (_usingWand)
                ToolBox.StopWandWaitGCD(UseWand, Smite);
            Thread.Sleep(Main._humanReflexTime);
            Lua.RunMacroText("/target player");
            Lua.RunMacroText("/cast Dispel Magic");
            _dispelTimer.Restart();
            ToolBox.WaitGlobalCoolDown(Smite);
            return;
        }

        // Vampiric Touch
        if (Target.GetDistance <= _maxRange && !Target.HaveBuff("Vampiric Touch") 
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
        if (_myManaPC > 10 && Target.GetDistance < _maxRange && Target.HealthPercent > 15
            && !Target.HaveBuff("Shadow Word: Pain"))
            if (Cast(ShadowWordPain))
                return;

        // Inner Fire
        if (!Me.HaveBuff("Inner Fire") && _settings.UseInnerFire && InnerFire.KnownSpell
            && _myManaPC > _innerManaSaveThreshold && Target.HealthPercent > _wandThreshold)
            if (Cast(InnerFire))
                return;

        // Shadowguard
        if (!Me.HaveBuff("Shadowguard") && _myManaPC > _innerManaSaveThreshold
            && _settings.UseShadowGuard && Target.HealthPercent > _wandThreshold)
            if (Cast(Shadowguard))
                return;

        // Shadow Protection
        if (!Me.HaveBuff("Shadow Protection") && _myManaPC > 70 && _settings.UseShadowProtection)
            if (Cast(ShadowProtection))
                return;

        // Devouring Plague
        if (!Target.HaveBuff("Devouring Plague") && Target.HealthPercent > 80)
            if (Cast(DevouringPlague))
                return;

        // Shadow Word Death
            if (_myManaPC > _innerManaSaveThreshold && Target.GetDistance < _maxRange 
            && _settings.UseShadowWordDeath && Target.HealthPercent < 15)
            if (Cast(ShadowWordDeath))
                return;

        // Mind Blast + Inner Focus
        if (!_inShadowForm && _myManaPC > _innerManaSaveThreshold && Target.GetDistance < _maxRange
            && Target.HealthPercent > 50 && _mindBlastCD <= 0 && (Target.HealthPercent > _wandThreshold || !_iCanUseWand))
        {
            if (InnerFocus.KnownSpell && _innerFocusCD <= 0)
                Cast(InnerFocus);

            if (Cast(MindBlast))
                return;
        }

        // Shadow Form Mind Blast + Inner Focus
        if (_inShadowForm && _myManaPC > _innerManaSaveThreshold && Target.GetDistance < _maxRange
            && _mindBlastCD <= 0 && Target.HealthPercent > _wandThreshold)
        {
            if (InnerFocus.KnownSpell && _innerFocusCD <= 0)
                Cast(InnerFocus);

            if (Cast(MindBlast))
                return;
        }

        // Mind Flay Range check
        if (_inShadowForm && !MindFlay.IsDistanceGood && (Me.HaveBuff("Power Word: Shield") || !_settings.UsePowerWordShield))
        {
            Main.LogDebug("Approaching to be in Mind Flay range");
            _goInMFRange = true;
            return;
        }

        // Mind FLay
        if ((Me.HaveBuff("Power Word: Shield") || !_settings.UsePowerWordShield) && MindFlay.IsDistanceGood 
            && _myManaPC > _innerManaSaveThreshold && Target.HealthPercent > _wandThreshold)
            if (Cast(MindFlay, false))
                return;

        // Low level Smite
        if (Me.Level < 5 && (Target.HealthPercent > 30 || Me.ManaPercentage > 80) && _myManaPC > _innerManaSaveThreshold 
            && Target.GetDistance < _maxRange)
            if (Cast(Smite, false))
                return;

        // Smite
        if (!_inShadowForm && _myManaPC > _innerManaSaveThreshold && Target.GetDistance < _maxRange
            && Me.Level >= 5 && Target.HealthPercent > 20 && (Target.HealthPercent > _settings.WandThreshold || !_iCanUseWand))
            if (Cast(Smite, false))
                return;

        // Use Wand
        if (!_usingWand && _iCanUseWand && Target.GetDistance <= _maxRange + 2)
        {
            Main.settingRange = _maxRange;
            if (Cast(UseWand, false))
                return;
        }

        // Go in melee because nothing else to do
        if (!_usingWand && !_iCanUseWand && Main.settingRange != _meleeRange && Target.IsAlive)
        {
            Main.Log("Going in melee");
            Main.settingRange = _meleeRange;
            return;
        }
    }

    public static void ShowConfiguration()
    {
        ZEPriestSettings.Load();
        ZEPriestSettings.CurrentSetting.ToForm();
        ZEPriestSettings.CurrentSetting.Save();
    }

    private static Spell Smite = new Spell("Smite");
    private static Spell LesserHeal = new Spell("Lesser Heal");
    private static Spell PowerWordFortitude = new Spell("Power Word: Fortitude");
    private static Spell PowerWordShield = new Spell("Power Word: Shield");
    private static Spell ShadowWordPain = new Spell("Shadow Word: Pain");
    private static Spell ShadowWordDeath = new Spell("Shadow Word: Death");
    private static Spell UseWand = new Spell("Shoot");
    private static Spell Renew = new Spell("Renew");
    private static Spell MindBlast = new Spell("Mind Blast");
    private static Spell InnerFire = new Spell("Inner Fire");
    private static Spell CureDisease = new Spell("Cure Disease");
    private static Spell PsychicScream = new Spell("Psychic Scream");
    private static Spell Heal = new Spell("Heal");
    private static Spell MindFlay = new Spell("Mind Flay");
    private static Spell HolyFire = new Spell("Holy Fire");
    private static Spell DispelMagic = new Spell("Dispel Magic");
    private static Spell FlashHeal = new Spell("Flash Heal");
    private static Spell VampiricEmbrace = new Spell("Vampiric Embrace");
    private static Spell Shadowguard = new Spell("Shadowguard");
    private static Spell ShadowProtection = new Spell("Shadow Protection");
    private static Spell Shadowform = new Spell("Shadowform");
    private static Spell VampiricTouch = new Spell("Vampiric Touch");
    private static Spell InnerFocus = new Spell("Inner Focus");
    private static Spell Shadowfiend = new Spell("Shadowfiend");
    private static Spell Silence = new Spell("Silence");
    private static Spell DivineSpirit = new Spell("Divine Spirit");
    private static Spell DevouringPlague = new Spell("Devouring Plague");
    private static Spell Cannibalize = new Spell("Cannibalize");
    private static Spell WillOfTheForsaken = new Spell("Will of the Forsaken");
    private static Spell Berserking = new Spell("Berserking");
    private static Spell Stoneform = new Spell("Stoneform");
    private static Spell GiftOfTheNaaru = new Spell("Gift of the Naaru");
    private static Spell ManaTap = new Spell("Mana Tap");
    private static Spell ArcaneTorrent = new Spell("Arcane Torrent");

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
        if (ObjectManager.Target.IsAlive || (!Fight.InFight && ObjectManager.Target.Guid < 1))
        {
            s.Launch();
            Usefuls.WaitIsCasting();
        }
        return true;
    }

    private static void CombatDebug(string s)
    {
        if (_settings.ActivateCombatDebug)
            Main.CombatDebug(s);
    }
}
