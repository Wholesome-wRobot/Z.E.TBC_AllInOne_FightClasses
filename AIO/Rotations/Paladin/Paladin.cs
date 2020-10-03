using System;
using System.Diagnostics;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using WholesomeTBCAIO.Helpers;
using WholesomeTBCAIO.Settings;
using wManager.Events;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeTBCAIO.Rotations.Paladin
{
    public class Paladin : IClassRotation
    {
        public static PaladinSettings settings;

        protected Stopwatch _purifyTimer = new Stopwatch();
        protected Stopwatch _cleanseTimer = new Stopwatch();
        protected WoWLocalPlayer Me = ObjectManager.Me;

        private static int _manaSavePercent;

        protected Paladin specialization;

        public void Initialize(IClassRotation specialization)
        {
            Logger.Log("Initialized");
            settings = PaladinSettings.Current;

            this.specialization = specialization as Paladin;
            Talents.InitTalents(settings);

            _manaSavePercent = settings.ManaSaveLimitPercent;
            if (_manaSavePercent < 20)
                _manaSavePercent = 20;

            // Fight end
            FightEvents.OnFightEnd += (guid) =>
            {
                _purifyTimer.Reset();
                _cleanseTimer.Reset();
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
                        && !Me.IsDeadMe 
                        && !Main.HMPrunningAway)
                    {
                        specialization.BuffRotation();

                        if (Fight.InFight 
                            && Me.Target > 0UL 
                            && ObjectManager.Target.IsAttackable)
                        {
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
            // Holy Light
            if (Me.HealthPercent < 50 && !Fight.InFight && !Me.IsMounted && HolyLight.IsSpellUsable)
            {
                Lua.RunMacroText("/target player");
                Cast(HolyLight);
                Lua.RunMacroText("/cleartarget");
            }

            // Flash of Light
            if (Me.HealthPercent < 75 && !Fight.InFight && settings.FlashHealBetweenFights
                && !Me.IsMounted && FlashOfLight.IsSpellUsable)
            {
                Lua.RunMacroText("/target player");
                Cast(FlashOfLight);
                Lua.RunMacroText("/cleartarget");
            }

            // Crusader Aura
            if (Me.IsMounted && CrusaderAura.KnownSpell && !Me.HaveBuff("Crusader Aura") && !Fight.InFight)
                Cast(CrusaderAura);

            // Sanctity Aura
            if (!Me.HaveBuff("Sanctity Aura") && SanctityAura.KnownSpell && !Me.IsMounted)
                Cast(SanctityAura);

            // Retribution Aura
            if (!Me.HaveBuff("Retribution Aura") && !SanctityAura.KnownSpell && RetributionAura.KnownSpell && !Me.IsMounted)
                Cast(SanctityAura);

            // Blessing of Wisdom
            if (settings.UseBlessingOfWisdom && !Me.HaveBuff("Blessing of Wisdom")
                && !Me.IsMounted && BlessingOfWisdom.IsSpellUsable)
            {
                Lua.RunMacroText("/target player");
                Cast(BlessingOfWisdom);
                Lua.RunMacroText("/cleartarget");
            }

            // Blessing of Might
            if (!settings.UseBlessingOfWisdom && !Me.HaveBuff("Blessing of Might")
                && !Me.IsMounted && BlessingOfMight.IsSpellUsable)
            {
                Lua.RunMacroText("/target player");
                Cast(BlessingOfMight);
                Lua.RunMacroText("/cleartarget");
            }
        }


        protected virtual void CombatRotation()
        {
            WoWUnit Target = ObjectManager.Target;

            ToolBox.CheckAutoAttack(Attack);

            // Purify
            if ((ToolBox.HasPoisonDebuff() || ToolBox.HasDiseaseDebuff()) && Purify.IsSpellUsable &&
                (_purifyTimer.ElapsedMilliseconds > 10000 || _purifyTimer.ElapsedMilliseconds <= 0))
            {
                _purifyTimer.Restart();
                Thread.Sleep(Main.humanReflexTime);
                Lua.RunMacroText("/target player");
                Cast(Purify);
                Lua.RunMacroText("/cleartarget");
            }

            // Cleanse
            if (ToolBox.HasMagicDebuff() && (_cleanseTimer.ElapsedMilliseconds > 10000 || _cleanseTimer.ElapsedMilliseconds <= 0)
                && Cleanse.IsSpellUsable)
            {
                _cleanseTimer.Restart();
                Thread.Sleep(Main.humanReflexTime);
                Lua.RunMacroText("/target player");
                Cast(Cleanse);
                Lua.RunMacroText("/cleartarget");
            }

            // Mana Tap
            if (Target.Mana > 0 && Target.ManaPercentage > 10)
                Cast(ManaTap);

            // Arcane Torrent
            if (Me.HaveBuff("Mana Tap") && Me.ManaPercentage < 50
                || Target.IsCast && Target.GetDistance < 8)
                Cast(ArcaneTorrent);

            // Gift of the Naaru
            if (ObjectManager.GetNumberAttackPlayer() > 1 && Me.HealthPercent < 50)
                Cast(GiftOfTheNaaru);

            // Stoneform
            if (ToolBox.HasPoisonDebuff() || ToolBox.HasDiseaseDebuff() || Me.HaveBuff("Bleed"))
                Cast(Stoneform);

            // Devotion Aura multi
            if (ObjectManager.GetNumberAttackPlayer() > 1 && settings.DevoAuraOnMulti &&
                !Me.HaveBuff("Devotion Aura"))
                Cast(DevotionAura);

            // Devotion Aura
            if (!Me.HaveBuff("Devotion Aura") && !SanctityAura.KnownSpell && !RetributionAura.KnownSpell)
                Cast(DevotionAura);

            // Sanctity Aura
            if (!Me.HaveBuff("Sanctity Aura") && SanctityAura.KnownSpell && ObjectManager.GetNumberAttackPlayer() <= 1)
                Cast(SanctityAura);

            // Retribution Aura
            if (!Me.HaveBuff("Retribution Aura") && !SanctityAura.KnownSpell && RetributionAura.KnownSpell
                && ObjectManager.GetNumberAttackPlayer() <= 1)
                Cast(SanctityAura);

            // Lay on Hands
            if (Me.HealthPercent < 10)
                Cast(LayOnHands);

            // Avenging Wrath
            if (Me.ManaPercentage > _manaSavePercent && ObjectManager.GetNumberAttackPlayer() > 1)
                Cast(AvengingWrath);

            // Hammer of Justice
            if (Me.HealthPercent < 50 && Me.ManaPercentage > _manaSavePercent)
                Cast(HammerOfJustice);

            // Exorcism
            if (Target.CreatureTypeTarget == "Undead" || Target.CreatureTypeTarget == "Demon"
                && settings.UseExorcism)
                Cast(Exorcism);

            // Judgement (Crusader)
            if (Me.HaveBuff("Seal of the Crusader") && Target.GetDistance < 10)
            {
                Cast(Judgement);
                Thread.Sleep(200);
            }

            // Judgement
            if ((Me.HaveBuff("Seal of Righteousness") || Me.HaveBuff("Seal of Command"))
                && Target.GetDistance < 10
                && (Me.ManaPercentage >= _manaSavePercent || Me.HaveBuff("Seal of the Crusader")))
                Cast(Judgement);

            // Seal of the Crusader
            if (!Target.HaveBuff("Judgement of the Crusader") && !Me.HaveBuff("Seal of the Crusader")
                && Me.ManaPercentage > _manaSavePercent - 20 && Target.IsAlive && settings.UseSealOfTheCrusader)
                Cast(SealOfTheCrusader);

            // Seal of Righteousness
            if (!Me.HaveBuff("Seal of Righteousness") && !Me.HaveBuff("Seal of the Crusader") && Target.IsAlive &&
                (Target.HaveBuff("Judgement of the Crusader") || Me.ManaPercentage > _manaSavePercent || !settings.UseSealOfTheCrusader)
                && (!settings.UseSealOfCommand || !SealOfCommand.KnownSpell))
                Cast(SealOfRighteousness);

            // Seal of Command
            if (!Me.HaveBuff("Seal of Command") && !Me.HaveBuff("Seal of the Crusader") && Target.IsAlive &&
                (Target.HaveBuff("Judgement of the Crusader") || Me.ManaPercentage > _manaSavePercent || !settings.UseSealOfTheCrusader)
                && settings.UseSealOfCommand && SealOfCommand.KnownSpell)
                Cast(SealOfCommand);

            // Seal of Command Rank 1
            if (!Me.HaveBuff("Seal of Righteousness") && !Me.HaveBuff("Seal of the Crusader") &&
                !Me.HaveBuff("Seal of Command") && !SealOfCommand.IsSpellUsable && !SealOfRighteousness.IsSpellUsable
                && SealOfCommand.KnownSpell && Me.Mana < _manaSavePercent)
                Lua.RunMacroText("/cast Seal of Command(Rank 1)");

            // Holy Light / Flash of Light
            if (Me.HealthPercent < 50 && (Target.HealthPercent > 15 || Me.HealthPercent < 25) && settings.HealDuringCombat)
            {
                if (!HolyLight.IsSpellUsable)
                {
                    if (Me.HealthPercent < 20)
                        Cast(DivineShield);
                    Cast(FlashOfLight);
                }
                Cast(HolyLight);
            }

            // Crusader Strike
            if (Me.ManaPercentage > 10)
                Cast(CrusaderStrike);

            // Hammer of Wrath
            if (settings.UseHammerOfWrath)
                Cast(HammerOfWrath);
        }

        protected Spell SealOfRighteousness = new Spell("Seal of Righteousness");
        protected Spell SealOfTheCrusader = new Spell("Seal of the Crusader");
        protected Spell SealOfCommand = new Spell("Seal of Command");
        protected Spell HolyLight = new Spell("Holy Light");
        protected Spell DevotionAura = new Spell("Devotion Aura");
        protected Spell BlessingOfMight = new Spell("Blessing of Might");
        protected Spell Judgement = new Spell("Judgement");
        protected Spell LayOnHands = new Spell("Lay on Hands");
        protected Spell HammerOfJustice = new Spell("Hammer of Justice");
        protected Spell RetributionAura = new Spell("Retribution Aura");
        protected Spell Exorcism = new Spell("Exorcism");
        protected Spell ConcentrationAura = new Spell("Concentration Aura");
        protected Spell SanctityAura = new Spell("Sanctity Aura");
        protected Spell FlashOfLight = new Spell("Flash of Light");
        protected Spell BlessingOfWisdom = new Spell("Blessing of Wisdom");
        protected Spell DivineShield = new Spell("Divine Shield");
        protected Spell Cleanse = new Spell("Cleanse");
        protected Spell Purify = new Spell("Purify");
        protected Spell CrusaderStrike = new Spell("Crusader Strike");
        protected Spell HammerOfWrath = new Spell("Hammer of Wrath");
        protected Spell Attack = new Spell("Attack");
        protected Spell CrusaderAura = new Spell("Crusader Aura");
        protected Spell AvengingWrath = new Spell("Avenging Wrath");
        protected Spell Stoneform = new Spell("Stoneform");
        protected Spell GiftOfTheNaaru = new Spell("Gift of the Naaru");
        protected Spell ManaTap = new Spell("Mana Tap");
        protected Spell ArcaneTorrent = new Spell("Arcane Torrent");

        protected void Cast(Spell s)
        {
            CombatDebug("In cast for " + s.Name);
            if (s.IsSpellUsable && s.KnownSpell)
                s.Launch();
        }

        protected void CombatDebug(string s)
        {
            if (settings.ActivateCombatDebug)
                Logger.CombatDebug(s);
        }
    }
}