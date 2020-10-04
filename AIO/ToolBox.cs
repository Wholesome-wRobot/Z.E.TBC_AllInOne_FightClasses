using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WholesomeTBCAIO.Helpers;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeTBCAIO
{
    public class ToolBox
    {

        #region Combat

        // Stops using wand and waits for its CD to be over
        public static void StopWandWaitGCD(Spell wandSpell, Spell basicSpell)
        {
            wandSpell.Launch();
            int c = 0;
            while (!basicSpell.IsSpellUsable)
            {
                c += 50;
                Thread.Sleep(50);
                if (c >= 1500)
                    return;
            }
            Logger.LogDebug("Waited for GCD : " + c);
            if (c >= 1500)
                wandSpell.Launch();
        }

        // Returns the cooldown of the spell passed as argument
        public static float GetSpellCooldown(string spellName)
        {
            return Lua.LuaDoString<float>("local startTime, duration, enable = GetSpellCooldown('" + spellName + "'); return duration - (GetTime() - startTime)");
        }

        // Returns the cost of the spell passed as argument
        public static int GetSpellCost(string spellName)
        {
            return Lua.LuaDoString<int>("local name, rank, icon, cost, isFunnel, powerType, castTime, minRange, maxRange = GetSpellInfo('" + spellName + "'); return cost");
        }

        // Returns the cast time in milliseconds of the spell passed as argument
        public static float GetSpellCastTime(string spellName)
        {
            return Lua.LuaDoString<float>("local name, rank, icon, cost, isFunnel, powerType, castTime, minRange, maxRange = GetSpellInfo('" + spellName + "'); return castTime");
        }

        // Reactivates auto attack if it's off. Must pass the Attack spell as argument
        public static void CheckAutoAttack(Spell attack)
        {
            bool _autoAttacking = Lua.LuaDoString<bool>("isAutoRepeat = false; if IsCurrentSpell('Attack') then isAutoRepeat = true end", "isAutoRepeat");
            if (!_autoAttacking && ObjectManager.Target.IsAlive)
            {
                Logger.LogDebug("Re-activating attack");
                attack.Launch();
            }
        }

        // Returns whether the unit can bleed or be poisoned
        public static bool CanBleed(WoWUnit unit)
        {
            return unit.CreatureTypeTarget != "Elemental" && unit.CreatureTypeTarget != "Mechanical";
        }

        // Returns whether the player is poisoned
        public static bool HasPoisonDebuff()
        {
            return Lua.LuaDoString<bool>
                (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if d == 'Poison' then
                return true
                end
            end");
        }

        // Returns whether the player has a disease
        public static bool HasDiseaseDebuff()
        {
            return Lua.LuaDoString<bool>
                (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if d == 'Disease' then
                return true
                end
            end");
        }

        // Returns whether the player has a curse
        public static bool HasCurseDebuff()
        {
            return Lua.LuaDoString<bool>
                (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if d == 'Curse' then
                return true
                end
            end");
        }

        // Returns whether the player has a magic debuff
        public static bool HasMagicDebuff()
        {
            return Lua.LuaDoString<bool>
                (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if d == 'Magic' then
                return true
                end
            end");
        }

        // Returns the type of debuff the player has as a string
        public static string GetDebuffType()
        {
            return Lua.LuaDoString<string>
                (@"for i=1,25 do 
	            local _, _, _, _, d  = UnitDebuff('player',i);
	            if (d == 'Poison' or d == 'Magic' or d == 'Curse' or d == 'Disease') then
                return d
                end
            end");
        }

        // Returns whether the player has the debuff passed as a string (ex: Weakened Soul)
        public static bool HasDebuff(string debuffName)
        {
            return Lua.LuaDoString<bool>
                ($"for i=1,25 do " +
                    "local n, _, _, _, _  = UnitDebuff('player',i); " +
                    "if n == '" + debuffName + "' then " +
                    "return true " +
                    "end " +
                "end");
        }

        // Returns the time left on a buff in seconds, buff name is passed as string
        public static int BuffTimeLeft(string buffName)
        {
            return Lua.LuaDoString<int>
                ($"for i=1,25 do " +
                    "local n, _, _, _, _, duration, _  = UnitBuff('player',i); " +
                    "if n == '" + buffName + "' then " +
                    "return duration " +
                    "end " +
                "end");
        }

        // Returns true if the enemy is either casting or channeling (good for interrupts)
        public static bool EnemyCasting()
        {
            int channelTimeLeft = Lua.LuaDoString<int>(@"local spell, _, _, _, endTimeMS = UnitChannelInfo('target')
                                    if spell then
                                     local finish = endTimeMS / 1000 - GetTime()
                                     return finish
                                    end");
            if (channelTimeLeft < 0 || ObjectManager.Target.CastingTimeLeft > Usefuls.Latency)
                return true;
            return false;
        }

        // Waits for GlobalCooldown to be off, must pass the most basic spell avalailable at lvl1 (ex: Smite for priest)
        public static void WaitGlobalCoolDown(Spell s)
        {
            int c = 0;
            while (!s.IsSpellUsable)
            {
                c += 50;
                Thread.Sleep(50);
                if (c >= 2000)
                    return;
            }
            Logger.LogDebug("Waited for GCD : " + c);
        }

        #endregion

        #region Misc

        // Returns whether units, hostile or not, are close to the player. Distance must be passed as argument
        public static bool CheckIfEnemiesClose(float distance)
        {
            List<WoWUnit> surroundingEnemies = ObjectManager.GetObjectWoWUnit();
            WoWUnit closestUnit = null;
            float closestUnitDistance = 100;

            foreach (WoWUnit unit in surroundingEnemies)
            {
                float distanceFromTarget = unit.Position.DistanceTo(ObjectManager.Me.Position);

                if (unit.IsAlive && !unit.IsTapDenied && unit.IsValid && !unit.IsTaggedByOther && !unit.PlayerControlled
                    && unit.IsAttackable && distanceFromTarget < closestUnitDistance && unit.Guid != ObjectManager.Target.Guid)
                {
                    closestUnit = unit;
                    closestUnitDistance = distanceFromTarget;
                }
            }

            if (closestUnit != null && closestUnitDistance < distance)
            {
                Logger.LogDebug("Enemy close: " + closestUnit.Name);
                return true;
            }
            return false;
        }

        // Returns whether hostile units are close to the target. Target and distance must be passed as argument
        public static bool CheckIfEnemiesOnPull(WoWUnit target, float distance)
        {
            List<WoWUnit> surroundingEnemies = ObjectManager.GetObjectWoWUnit();
            WoWUnit closestUnit = null;
            float closestUnitDistance = 100;

            foreach (WoWUnit unit in surroundingEnemies)
            {
                bool flagHostile = unit.Reaction.ToString().Equals("Hostile");
                float distanceFromTarget = unit.Position.DistanceTo(target.Position);

                if (unit.IsAlive
                    && !unit.IsTapDenied
                    && unit.IsValid
                    && !unit.IsTaggedByOther
                    && !unit.PlayerControlled
                    && unit.IsAttackable
                    && distanceFromTarget < closestUnitDistance
                    && flagHostile
                    && unit.Guid != target.Guid)
                {
                    closestUnit = unit;
                    closestUnitDistance = distanceFromTarget;
                }
            }

            if (closestUnit != null && closestUnitDistance < distance)
            {
                Logger.Log("Enemy too close: " + closestUnit.Name + ", pulling from distance");
                return true;
            }
            return false;
        }

        // Gets Character's specialization (talents)
        public static string GetSpec()
        {
            var Talents = new Dictionary<string, int>();
            for (int i = 1; i <= 3; i++)
            {
                Talents.Add(
                    Lua.LuaDoString<string>($"local name, iconTexture, pointsSpent = GetTalentTabInfo({i}); return name"),
                    Lua.LuaDoString<int>($"local name, iconTexture, pointsSpent = GetTalentTabInfo({i}); return pointsSpent")
                );
            }
            var highestTalents = Talents.Max(x => x.Value);
            return Talents.Where(t => t.Value == highestTalents).FirstOrDefault().Key;
        }

        // Returns the latency
        public static int GetLatency()
        {
            int worldLatency = Lua.LuaDoString<int>($"local down, up, lagHome, lagWorld = GetNetStats(); return lagWorld");
            int homeLatency = Lua.LuaDoString<int>($"local down, up, lagHome, lagWorld = GetNetStats(); return lagHome");
            return worldLatency + homeLatency;
        }

        #endregion

        #region Items

        // Return Main hand weapon type as a string
        public static string GetMHWeaponType()
        {
            return Lua.LuaDoString<string>(@"local _, _, _, _, _, _, weapontype = 
                                            GetItemInfo(GetInventoryItemLink('player', 16)); return weapontype;");
        }

        // Check if range weapon (wand, bow, gun) equipped
        public static bool HaveRangedWeaponEquipped()
        {
            return ObjectManager.Me.GetEquipedItemBySlot(wManager.Wow.Enums.InventorySlot.INVSLOT_RANGED) != 0;
        }

        // Deletes item passed as string
        public static void LuaDeleteItem(string item)
        {
            Lua.LuaDoString("for bag = 0, 4, 1 do for slot = 1, 32, 1 do local name = GetContainerItemLink(bag, slot); " +
                "if name and string.find(name, \"" + item + "\") then PickupContainerItem(bag, slot); " +
                "DeleteCursorItem(); end; end; end", false);
        }

        // Count the amount of the specified item stacks in your bags
        public static int CountItemStacks(string itemArg)
        {
            return Lua.LuaDoString<int>("local count = GetItemCount('" + itemArg + "'); return count");
        }

        // Checks if you have any of the listed items in your bags
        public static bool HaveOneInList(List<string> list)
        {
            List<WoWItem> _bagItems = Bag.GetBagItem();
            bool _haveItem = false;
            foreach (WoWItem item in _bagItems)
            {
                if (list.Contains(item.Name))
                    _haveItem = true;
            }
            return _haveItem;
        }

        // Get item ID in bag from a list passed as argument (good to check CD)
        public static int GetItemID(List<string> list)
        {
            List<WoWItem> _bagItems = Bag.GetBagItem();
            foreach (WoWItem item in _bagItems)
                if (list.Contains(item.Name))
                    return item.Entry;

            return 0;
        }

        // Get item ID in bag from a string passed as argument (good to check CD)
        public static int GetItemID(string itemName)
        {
            List<WoWItem> _bagItems = Bag.GetBagItem();
            foreach (WoWItem item in _bagItems)
                if (itemName.Equals(item))
                    return item.Entry;

            return 0;
        }

        // Get item Cooldown (must pass item string as arg)
        public static int GetItemCooldown(string itemName)
        {
            int entry = GetItemID(itemName);
            List<WoWItem> _bagItems = Bag.GetBagItem();
            foreach (WoWItem item in _bagItems)
                if (entry == item.Entry)
                    return Lua.LuaDoString<int>("local startTime, duration, enable = GetItemCooldown(" + entry + "); " +
                        "return duration - (GetTime() - startTime)");

            Logger.Log("Couldn't find item" + itemName);
            return 0;
        }

        // Get item Cooldown from list (must pass item list as arg)
        public static int GetItemCooldown(List<string> itemList)
        {
            int entry = GetItemID(itemList);
            List<WoWItem> _bagItems = Bag.GetBagItem();
            foreach (WoWItem item in _bagItems)
                if (entry == item.Entry)
                    return Lua.LuaDoString<int>("local startTime, duration, enable = GetItemCooldown(" + entry + "); " +
                        "return duration - (GetTime() - startTime)");

            Logger.Log("Couldn't find item");
            return 0;
        }

        // Uses the first item found in your bags that matches any element from the list
        public static void UseFirstMatchingItem(List<string> list)
        {
            List<WoWItem> _bagItems = Bag.GetBagItem();
            foreach (WoWItem item in _bagItems)
            {
                if (list.Contains(item.Name))
                {
                    ItemsManager.UseItemByNameOrId(item.Name);
                    Logger.Log("Using " + item.Name);
                    return;
                }
            }
        }

        // Returns the item found in your bags that matches the latest element from the list
        public static string GetBestMatchingItem(List<string> list)
        {
            string _bestItem = null;
            int index = 0;

            List<WoWItem> _bagItems = Bag.GetBagItem();
            foreach (WoWItem item in _bagItems)
            {
                if (list.Contains(item.Name))
                {
                    int itemIndex = list.IndexOf(item.Name);
                    if (itemIndex >= index)
                        _bestItem = item.Name;
                }
            }
            return _bestItem;
        }

        #endregion

        #region Pet

        // Returns the index of the pet spell passed as argument
        public static int GetPetSpellIndex(string spellName)
        {
            int spellindex = Lua.LuaDoString<int>
                ($"for i=1,10 do " +
                    "local name, _, _, _, _, _, _ = GetPetActionInfo(i); " +
                    "if name == '" + spellName + "' then " +
                    "return i " +
                    "end " +
                "end");

            return spellindex;
        }

        // Returns the cooldown of the pet spell passed as argument
        public static int GetPetSpellCooldown(string spellName)
        {
            int _spellIndex = GetPetSpellIndex(spellName);
            return Lua.LuaDoString<int>("local startTime, duration, enable = GetPetActionCooldown(" + _spellIndex + "); return duration - (GetTime() - startTime)");
        }

        // Returns whether a pet spell is available (off cooldown)
        public static bool GetPetSpellReady(string spellName)
        {
            return GetPetSpellCooldown(spellName) <= 0;
        }

        // Casts the pet spell passed as argument
        public static void PetSpellCast(string spellName)
        {
            int spellIndex = GetPetSpellIndex(spellName);
            if (GetPetSpellReady(spellName))
                Lua.LuaDoString("CastPetAction(" + spellIndex + ");");
        }

        // Toggles Pet spell autocast (pass true as second argument to toggle on, or false to toggle off)
        public static void TogglePetSpellAuto(string spellName, bool toggle)
        {
            string spellIndex = GetPetSpellIndex(spellName).ToString();

            if (!spellIndex.Equals("0"))
            {
                bool autoCast = Lua.LuaDoString<bool>("local _, autostate = GetSpellAutocast(" + spellIndex + ", 'pet'); " +
                    "return autostate == 1") || Lua.LuaDoString<bool>("local _, autostate = GetSpellAutocast('" + spellName + "', 'pet'); " +
                    "return autostate == 1");

                if ((toggle && !autoCast) || (!toggle && autoCast))
                {
                    //Lua.LuaDoString("ToggleSpellAutocast(" + spellIndex + ", 'pet');");
                    Lua.LuaDoString("ToggleSpellAutocast('" + spellName + "', 'pet');");
                }
            }
        }

        #endregion

        #region Movement

        // get the position behind the target
        public static Vector3 BackofVector3(Vector3 from, WoWUnit targetObject, float radius)
        {
            if (from != null && from != Vector3.Empty)
            {
                float rotation = -robotManager.Helpful.Math.DegreeToRadian(robotManager.Helpful.Math.RadianToDegree(targetObject.Rotation) + 90);
                return new Vector3((System.Math.Sin(rotation) * radius) + from.X, (System.Math.Cos(rotation) * radius) + from.Y, from.Z);
            }
            return new Vector3(0, 0, 0);
        }

        // Determines if me is behind the Target
        public static bool MeBehindTarget()
        {
            var target = ObjectManager.Target;

            float Pi = (float)System.Math.PI;
            bool backLeft = false;
            bool backRight = false;
            float target_x = ObjectManager.Target.Position.X;
            float target_y = ObjectManager.Target.Position.Y;
            float target_r = ObjectManager.Target.Rotation;
            float player_x = ObjectManager.Me.Position.X;
            float player_y = ObjectManager.Me.Position.Y;
            float d = (float)System.Math.Atan2((target_y - player_y), (target_x - player_x));
            float r = d - target_r;

            if (r < 0) r = r + (Pi * 2);
            if (r > 1.5 * Pi) backLeft = true;
            if (r < 0.5 * Pi) backRight = true;
            if (backLeft || backRight) return true; else return false;
        }

        #endregion
    }
}