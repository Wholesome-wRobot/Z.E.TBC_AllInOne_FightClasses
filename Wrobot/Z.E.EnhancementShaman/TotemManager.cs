using robotManager.Helpful;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

class TotemManager
{
    private WoWLocalPlayer Me = ObjectManager.Me;
    private Vector3 _lastTotemPosition = null;

    private Spell StoneclawTotem = new Spell("Stoneclaw Totem");
    private Spell StrengthOfEarthTotem = new Spell("Strength of Earth Totem");
    private Spell StoneskinTotem = new Spell("Stoneskin Totem");
    private Spell SearingTotem = new Spell("Searing Totem");
    internal Spell TotemicCall = new Spell("Totemic Call");
    private Spell ManaSpringTotem = new Spell("Mana Spring Totem");
    private Spell MagmaTotem = new Spell("Magma Totem");
    private Spell GraceOfAirTotem = new Spell("Grace of Air Totem");
    private Spell EarthElementalTotem = new Spell("Earth Elemental Totem");

    internal bool CastTotems()
    {
        if (CastWaterTotem())
            return true;
        if (CastEarthTotem())
            return true;
        if (CastFireTotem())
            return true;
        if (CastAirTotem())
            return true;
        return false;
    }

    internal void CheckForTotemicCall()
    {
        if (Shaman._settings.UseTotemicCall)
        {
            bool haveEarthTotem = Lua.LuaDoString<string>(@"local _, totemName, _, _ = GetTotemInfo(2); return totemName;").Contains("Totem");
            bool haveFireTotem = Lua.LuaDoString<string>(@"local _, totemName, _, _ = GetTotemInfo(1); return totemName;").Contains("Totem");
            bool haveWindTotem = Lua.LuaDoString<string>(@"local _, totemName, _, _ = GetTotemInfo(4); return totemName;").Contains("Totem");
            bool haveWaterTotem = Lua.LuaDoString<string>(@"local _, totemName, _, _ = GetTotemInfo(3); return totemName;").Contains("Totem");
            bool haveTotem = haveEarthTotem || haveFireTotem || haveWaterTotem || haveWindTotem;

            if (_lastTotemPosition != null && haveTotem && _lastTotemPosition.DistanceTo(Me.Position) > 17
                && !Me.HaveBuff("Ghost Wolf") && !Me.IsMounted && !Me.IsCast)
                Cast(TotemicCall);
        }
    }

    internal bool CastEarthTotem()
    {
        if (Shaman._settings.UseEarthTotems)
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

            // Strenght of Earth totem
            if ((!Shaman._settings.UseStoneSkinTotem || !StoneskinTotem.KnownSpell)
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
            if ((Shaman._settings.UseStoneSkinTotem || !StrengthOfEarthTotem.KnownSpell)
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

    internal bool CastFireTotem()
    {
        if (Shaman._settings.UseFireTotems)
        {
            string currentFireTotem = Lua.LuaDoString<string>
                (@"local haveTotem, totemName, startTime, duration = GetTotemInfo(1); return totemName;");

            // Magma Totem
            if (ObjectManager.GetNumberAttackPlayer() > 1 
                && Me.ManaPercentage > Shaman._mediumManaThreshold 
                && ObjectManager.Target.GetDistance < 10
                && !currentFireTotem.Contains("Magma Totem") 
                && Shaman._settings.UseMagmaTotem)
            {
                if (Cast(MagmaTotem))
                    return true;
            }

            // Searing Totem
            if ((!currentFireTotem.Contains("Searing Totem") || Shaman._fireTotemPosition == null || Me.Position.DistanceTo(Shaman._fireTotemPosition) > 15f)
                && ObjectManager.Target.GetDistance < 10 
                && !currentFireTotem.Contains("Magma Totem"))
            {
                if (Cast(SearingTotem))
                {
                    Shaman._fireTotemPosition = Me.Position;
                    return true;
                }
            }
        }
        return false;
    }

    internal bool CastAirTotem()
    {
        if (Shaman._settings.UseAirTotems)
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

    internal bool CastWaterTotem()
    {
        if (Shaman._settings.UseWaterTotems)
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

    internal bool Cast(Spell s)
    {
        Main.LogDebug("Into Totem Cast() for " + s.Name);

        if (!s.IsSpellUsable || !s.KnownSpell || Me.IsCast)
            return false;

        s.Launch();

        if (s.Name.Contains(" Totem"))
            _lastTotemPosition = Me.Position;

        return true;
    }
}
