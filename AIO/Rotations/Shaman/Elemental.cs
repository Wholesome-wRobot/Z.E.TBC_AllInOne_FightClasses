using System.Threading;
using WholesomeTBCAIO.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeTBCAIO.Rotations.Shaman
{
    public class Elemental : Shaman
    {
        protected override void BuffRotation()
        {
            base.BuffRotation();
        }

        protected override void Pull()
        {
            base.Pull();

            // Totems
            if (Me.ManaPercentage > _lowManaThreshold
                && ObjectManager.Target.GetDistance < 30)
                if (totemManager.CastTotems(specialization))
                    return;

            // Elemental Mastery
            if (!Me.HaveBuff("Elemental Mastery"))
                if (Cast(ElementalMastery))
                    return;

            // Lightning Bolt
            if (ObjectManager.Target.GetDistance <= _pullRange)
            {
                if (Cast(LightningBolt))
                    return;
            }
        }

        protected override void CombatRotation()
        {
            base.CombatRotation();

            bool _shouldBeInterrupted = false;
            WoWUnit Target = ObjectManager.Target;

            // Check if we need to interrupt
            int channelTimeLeft = Lua.LuaDoString<int>(@"local spell, _, _, _, endTimeMS = UnitChannelInfo('target')
                                    if spell then
                                     local finish = endTimeMS / 1000 - GetTime()
                                     return finish
                                    end");
            if (channelTimeLeft < 0 || Target.CastingTimeLeft > ToolBox.GetLatency())
                _shouldBeInterrupted = true;

            // Get in Shock Range
            if (_fightingACaster
                && RangeManager.GetRange() > 18)
            {
                RangeManager.SetRange(18);
                return;
            }

            // Earth Shock Interupt
            if (_shouldBeInterrupted
                && Target.GetDistance < 19f)
            {
                if (!_casterEnemies.Contains(Target.Name))
                    _casterEnemies.Add(Target.Name);
                _fightingACaster = true;
                Thread.Sleep(Main.humanReflexTime);
                if (Cast(EarthShock))
                    return;
            }

            // Frost Shock
            if ((Target.CreatureTypeTarget == "Humanoid" || Target.Name.Contains("Plainstrider"))
                && settings.ENFrostShockHumanoids
                && Target.HealthPercent < 40
                && !Target.HaveBuff("Frost Shock"))
                if (Cast(FrostShock))
                    return;

            // Totems
            if (Me.ManaPercentage > _lowManaThreshold
                && Target.GetDistance < 20)
                if (totemManager.CastTotems(specialization))
                    return;

            // Earth Shock DPS
            if (Target.GetDistance < 19f
                && (!FlameShock.KnownSpell || !settings.UseFlameShock)
                && !_fightingACaster
                && Target.HealthPercent > 25
                && Me.ManaPercentage > settings.ELShockDPSMana)
                if (Cast(EarthShock))
                    return;

            // Flame Shock DPS
            if (Target.GetDistance < 19f
                && !Target.HaveBuff("Flame Shock")
                && Target.HealthPercent > 20
                && !_fightingACaster
                && settings.UseFlameShock
                && Me.ManaPercentage > settings.ELShockDPSMana)
                if (Cast(FlameShock))
                    return;

            // Lightning Bolt
            if (ObjectManager.Target.GetDistance <= _pullRange
                && (Target.HealthPercent > settings.ELLBHealthThreshold || Me.HaveBuff("Clearcasting") || Me.HaveBuff("Focused Casting"))
                && Me.ManaPercentage > 15)
            {
                if (Cast(LightningBolt))
                    return;
            }

            // Default melee
            if (RangeManager.GetRange() > 10)
            {
                Logger.Log("Going in melee because nothing else to do");
                RangeManager.SetRangeToMelee();
            }
        }
    }
}
