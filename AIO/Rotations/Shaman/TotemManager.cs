using robotManager.Helpful;
using WholesomeTBCAIO.Helpers;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeTBCAIO.Rotations.Shaman
{
    public class TotemManager
    {
        private WoWLocalPlayer Me = ObjectManager.Me;
        private Vector3 _lastTotemPosition = null;
        private Vector3 _fireTotemPosition = null;

        private Spell StoneclawTotem = new Spell("Stoneclaw Totem");
        private Spell StrengthOfEarthTotem = new Spell("Strength of Earth Totem");
        private Spell StoneskinTotem = new Spell("Stoneskin Totem");
        private Spell SearingTotem = new Spell("Searing Totem");
        private Spell TotemicCall = new Spell("Totemic Call");
        private Spell ManaSpringTotem = new Spell("Mana Spring Totem");
        private Spell MagmaTotem = new Spell("Magma Totem");
        private Spell GraceOfAirTotem = new Spell("Grace of Air Totem");
        private Spell EarthElementalTotem = new Spell("Earth Elemental Totem");
        private Spell TotemOfWrath = new Spell("Totem of Wrath");

        public bool CastTotems(IClassRotation spec)
        {
            if (CastWaterTotem(spec))
                return true;
            if (CastEarthTotem(spec))
                return true;
            if (CastFireTotem(spec))
                return true;
            if (CastAirTotem(spec))
                return true;
            return false;
        }

        public void CheckForTotemicCall()
        {
            if (Shaman.settings.UseTotemicCall)
            {
                bool haveEarthTotem = Lua.LuaDoString<string>(@"local _, totemName, _, _ = GetTotemInfo(2); return totemName;").Contains("Totem");
                bool haveFireTotem = Lua.LuaDoString<string>(@"local _, totemName, _, _ = GetTotemInfo(1); return totemName;").Contains("Totem");
                bool haveWindTotem = Lua.LuaDoString<string>(@"local _, totemName, _, _ = GetTotemInfo(4); return totemName;").Contains("Totem");
                bool haveWaterTotem = Lua.LuaDoString<string>(@"local _, totemName, _, _ = GetTotemInfo(3); return totemName;").Contains("Totem");
                bool haveTotem = haveEarthTotem || haveFireTotem || haveWaterTotem || haveWindTotem;

                if (_lastTotemPosition != null 
                    && haveTotem 
                    && _lastTotemPosition.DistanceTo(Me.Position) > 17
                    && !Me.HaveBuff("Ghost Wolf") 
                    && !Me.IsMounted 
                    && !Me.IsCast)
                    Cast(TotemicCall);
            }
        }

        private bool CastEarthTotem(IClassRotation spec)
        {
            string currentEarthTotem = Lua.LuaDoString<string>
                (@"local haveTotem, totemName, startTime, duration = GetTotemInfo(2); return totemName;");

            // Earth Elemental Totem on multiaggro
            if (ObjectManager.GetNumberAttackPlayer() > 1
                && EarthElementalTotem.KnownSpell
                && !currentEarthTotem.Contains("Stoneclaw Totem")
                && !currentEarthTotem.Contains("Earth Elemental Totem"))
            {
                {
                    if (Cast(EarthElementalTotem))
                        return true;
                }
            }

            // Stoneclaw on multiaggro
            if (ObjectManager.GetNumberAttackPlayer() > 1
                && StoneclawTotem.KnownSpell
                && !currentEarthTotem.Contains("Stoneclaw Totem")
                && !currentEarthTotem.Contains("Earth Elemental Totem"))
            {
                {
                    if (Cast(StoneclawTotem))
                        return true;
                }
            }

            if (Shaman.settings.UseEarthTotems)
            {
                // Strenght of Earth totem
                if (spec is Enhancement
                    && (!Shaman.settings.UseStoneSkinTotem || !StoneskinTotem.KnownSpell)
                    && !Me.HaveBuff("Strength of Earth")
                    && !currentEarthTotem.Contains("Stoneclaw Totem")
                    && !currentEarthTotem.Contains("Earth Elemental Totem"))
                {
                    {
                        if (Cast(StrengthOfEarthTotem))
                            return true;
                    }
                }

                // Stoneskin Totem
                if ((Shaman.settings.UseStoneSkinTotem || !StrengthOfEarthTotem.KnownSpell || spec is Elemental || ObjectManager.GetNumberAttackPlayer() > 1)
                    && !Me.HaveBuff("Stoneskin")
                    && !currentEarthTotem.Contains("Stoneclaw Totem")
                    && !currentEarthTotem.Contains("Earth Elemental Totem"))
                {
                    {
                        if (Cast(StoneskinTotem))
                            return true;
                    }
                }
            }
            return false;
        }

        private bool CastFireTotem(IClassRotation spec)
        {
            if (Shaman.settings.UseFireTotems)
            {
                string currentFireTotem = Lua.LuaDoString<string>
                    (@"local haveTotem, totemName, startTime, duration = GetTotemInfo(1); return totemName;");

                // Magma Totem
                if (ObjectManager.GetNumberAttackPlayer() > 1
                    && Me.ManaPercentage > 50
                    && ObjectManager.Target.GetDistance < 10
                    && !currentFireTotem.Contains("Magma Totem")
                    && Shaman.settings.UseMagmaTotem)
                {
                    if (Cast(MagmaTotem))
                        return true;
                }

                // Searing Totem
                if ((!currentFireTotem.Contains("Searing Totem") || _fireTotemPosition == null || Me.Position.DistanceTo(_fireTotemPosition) > 15f)
                    && ObjectManager.Target.GetDistance < 15
                    && !currentFireTotem.Contains("Magma Totem")
                    && !Shaman.settings.UseTotemOfWrath)
                {
                    if (Cast(SearingTotem))
                    {
                        _fireTotemPosition = Me.Position;
                        return true;
                    }
                }

                // Totem of Wrath
                if (!currentFireTotem.Contains("Totem of Wrath")
                    && !Me.HaveBuff("Totem of Wrath")
                    && Shaman.settings.UseTotemOfWrath)
                {
                    if (Cast(TotemOfWrath))
                        return true;
                }

            }
            return false;
        }

        private bool CastAirTotem(IClassRotation spec)
        {
            if (Shaman.settings.UseAirTotems)
            {
                string currentAirTotem = Lua.LuaDoString<string>
                    (@"local _, totemName, _, _ = GetTotemInfo(4); return totemName;");

                // Mana Spring Totem
                if (!Me.HaveBuff("Grace of Air"))
                {
                    if (Cast(GraceOfAirTotem))
                        return true;
                }
            }
            return false;
        }

        private bool CastWaterTotem(IClassRotation spec)
        {
            if (Shaman.settings.UseWaterTotems)
            {
                string currentWaterTotem = Lua.LuaDoString<string>
                    (@"local _, totemName, _, _ = GetTotemInfo(3); return totemName;");

                // Mana Spring Totem
                if (!Me.HaveBuff("Mana Spring"))
                {
                    if (Cast(ManaSpringTotem))
                        return true;
                }
            }
            return false;
        }

        private bool Cast(Spell s)
        {
            Logger.LogDebug("Into Totem Cast() for " + s.Name);

            if (!s.IsSpellUsable || !s.KnownSpell || Me.IsCast)
                return false;

            if (s.Name.Contains(" Totem") || s.Name.Contains("Totem of"))
                _lastTotemPosition = Me.Position;

            s.Launch();

            return true;
        }
    }
}