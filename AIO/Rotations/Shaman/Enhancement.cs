using System.Threading;
using WholesomeTBCAIO.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeTBCAIO.Rotations.Shaman
{
    public class Enhancement : Shaman
    {
        protected override void BuffRotation()
        {
            base.BuffRotation();
        }

        protected override void Pull()
        {
            // Melee ?
            if (_pullMeleeTimer.ElapsedMilliseconds <= 0
                && ObjectManager.Target.GetDistance <= _pullRange + 3)
                _pullMeleeTimer.Start();

            if (_pullMeleeTimer.ElapsedMilliseconds > 8000
                && !RangeManager.CurrentRangeIsMelee())
            {
                RangeManager.SetRangeToMelee();
                _pullMeleeTimer.Reset();
            }

            base.Pull();

            // Pull with Lightning Bolt
            if (ObjectManager.Target.GetDistance <= _pullRange
                && !RangeManager.CurrentRangeIsMelee())
            {
                // pull with rank one
                if (settings.ENPullRankOneLightningBolt
                    && LightningBolt.IsSpellUsable)
                {
                    MovementManager.StopMove();
                    Lua.RunMacroText("/cast Lightning Bolt(Rank 1)");
                }

                // pull with max rank
                if (settings.ENPullWithLightningBolt
                    && !settings.ENPullRankOneLightningBolt
                    && LightningBolt.IsSpellUsable)
                {
                    MovementManager.StopMove();
                    Lua.RunMacroText("/cast Lightning Bolt");
                }

                _pullAttempt++;
                Thread.Sleep(300);

                // Check if we're NOT casting
                if (!Me.IsCast)
                {
                    Logger.Log($"Pull attempt failed ({_pullAttempt})");
                    if (_pullAttempt > 3)
                    {
                        Logger.Log("Cast unsuccesful, going in melee");
                        RangeManager.SetRangeToMelee();
                    }
                    return;
                }

                // If we're casting
                Usefuls.WaitIsCasting();

                int limit = 1500;
                while (!Me.InCombatFlagOnly && limit > 0)
                {
                    Thread.Sleep(100);
                    limit -= 100;
                }
            }
        }

        protected override void CombatRotation()
        {
            bool _shouldBeInterrupted = false;

            WoWUnit Target = ObjectManager.Target;

            // Check Auto-Attacking
            ToolBox.CheckAutoAttack(Attack);

            // Check if we need to interrupt
            int channelTimeLeft = Lua.LuaDoString<int>(@"local spell, _, _, _, endTimeMS = UnitChannelInfo('target')
                                    if spell then
                                     local finish = endTimeMS / 1000 - GetTime()
                                     return finish
                                    end");
            if (channelTimeLeft < 0 || Target.CastingTimeLeft > ToolBox.GetLatency())
                _shouldBeInterrupted = true;

            // Melee ?
            if (_pullMeleeTimer.ElapsedMilliseconds > 0)
                _pullMeleeTimer.Reset();

            if (_meleeTimer.ElapsedMilliseconds <= 0
                && !RangeManager.CurrentRangeIsMelee())
                _meleeTimer.Start();

            if ((_shouldBeInterrupted || _meleeTimer.ElapsedMilliseconds > 8000)
                && !RangeManager.CurrentRangeIsMelee())
            {
                Logger.LogDebug("Going in melee range");
                if (!_casterEnemies.Contains(Target.Name))
                    _casterEnemies.Add(Target.Name);
                _fightingACaster = true;
                RangeManager.SetRangeToMelee();
                _meleeTimer.Stop();
            }

            // BASE ROTATION
            base.CombatRotation();

            // Shamanistic Rage
            if (Me.ManaPercentage < _mediumManaThreshold
                && (Target.HealthPercent > 80 && !settings.ENShamanisticRageOnMultiOnly || ObjectManager.GetNumberAttackPlayer() > 1))
                if (Cast(ShamanisticRage))
                    return;

            // Earth Shock Focused
            if (Me.HaveBuff("Focused")
                && Target.GetDistance < 19f)
                if (Cast(EarthShock))
                    return;

            // Frost Shock
            if ((Target.CreatureTypeTarget == "Humanoid" || Target.Name.Contains("Plainstrider"))
                && settings.ENFrostShockHumanoids
                && Target.HealthPercent < 40
                && !Target.HaveBuff("Frost Shock"))
                if (Cast(FrostShock))
                    return;

            // Earth Shock Interupt Rank 1
            if (_shouldBeInterrupted
                && Target.GetDistance < 19f
                && (settings.ENInterruptWithRankOne || Me.ManaPercentage <= _lowManaThreshold))
            {
                _fightingACaster = true;
                if (!_casterEnemies.Contains(Target.Name))
                    _casterEnemies.Add(Target.Name);
                Thread.Sleep(Main.humanReflexTime);
                Lua.RunMacroText("/cast Earth Shock(Rank 1)");
                return;
            }

            // Earth Shock Interupt
            if (_shouldBeInterrupted
                && Target.GetDistance < 19f
                && !settings.ENInterruptWithRankOne)
            {
                if (!_casterEnemies.Contains(Target.Name))
                    _casterEnemies.Add(Target.Name);
                _fightingACaster = true;
                Thread.Sleep(Main.humanReflexTime);
                if (Cast(EarthShock))
                    return;
            }

            // Water Shield
            if (!Me.HaveBuff("Water Shield")
                && !Me.HaveBuff("Lightning Shield")
                && (settings.UseWaterShield || !settings.UseLightningShield || Me.ManaPercentage <= _lowManaThreshold))
                if (Cast(WaterShield))
                    return;

            // Totems
            if (Me.ManaPercentage > 20
                && Target.GetDistance < 20)
                if (totemManager.CastTotems(specialization))
                    return;

            // Flame Shock DPS
            if (Me.ManaPercentage > _lowManaThreshold
                && Target.GetDistance < 19f
                && !Target.HaveBuff("Flame Shock")
                && Target.HealthPercent > 20
                && !_fightingACaster
                && settings.UseFlameShock)
                if (Cast(FlameShock))
                    return;

            // Stormstrike
            if (Me.ManaPercentage > _lowManaThreshold
                && Stormstrike.IsDistanceGood)
                if (Cast(Stormstrike))
                    return;

            // Earth Shock DPS
            if (Me.ManaPercentage > _lowManaThreshold
                && Target.GetDistance < 19f
                && !FlameShock.KnownSpell
                && Target.HealthPercent > 25
                && Me.ManaPercentage > 30)
                if (Cast(EarthShock))
                    return;

            // Low level lightning bolt
            if (!EarthShock.KnownSpell
                && Me.ManaPercentage > 30
                && Me.ManaPercentage > _lowManaThreshold
                && Target.GetDistance < 29f
                && Target.HealthPercent > 40)
                if (Cast(LightningBolt))
                    return;
        }
    }
}
