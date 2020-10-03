using System.Collections.Generic;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using wManager.Wow.Class;

namespace WholesomeTBCAIO.Rotations.Mage
{
    public class MageFoodManager
    {
        private List<WoWItem> _bagItems;
        private Spell ConjureWater = new Spell("Conjure Water");
        private Spell ConjureFood = new Spell("Conjure Food");
        private Spell ConjureManaAgate = new Spell("Conjure Mana Agate");
        private Spell ConjureManaJade = new Spell("Conjure Mana Jade");
        private Spell ConjureManaCitrine = new Spell("Conjure Mana Citrine");
        private Spell ConjureManaRuby = new Spell("Conjure Mana Ruby");
        private Spell ConjureManaEmerald = new Spell("Conjure Mana Emerald");
        public string ManaStone = "";

        private List<string> Drink()
        {
            return new List<string>
        {
            "Conjured Water",
            "Conjured Fresh Water",
            "Conjured Purified Water",
            "Conjured Spring Water",
            "Conjured Mineral Water",
            "Conjured Sparkling Water",
            "Conjured Crystal Water",
            "Conjured Mountain Spring Water",
            "Conjured Glacier Water"
        };
        }

        private List<string> Food()
        {
            return new List<string>
        {
            "Conjured Muffin",
            "Conjured Bread",
            "Conjured Rye",
            "Conjured Pumpernickel",
            "Conjured Sourdough",
            "Conjured Sweet Roll",
            "Conjured Cinnamon Roll",
            "Conjured Croissant"
        };
        }

        private List<string> ManaStones()
        {
            return new List<string>
        {
            "Mana Agate",
            "Mana Jade",
            "Mana Citrine",
            "Mana Ruby",
            "Mana Emerald"
        };
        }

        public void CheckIfEnoughFoodAndDrinks()
        {
            if (!Fight.InFight)
            {
                _bagItems = Bag.GetBagItem();
                int stacksWater = 0;
                int stacksFood = 0;
                foreach (WoWItem item in _bagItems)
                {
                    if (Drink().Contains(item.Name))
                    {
                        stacksWater += Lua.LuaDoString<int>("return GetItemCount(\"" + item.Name + "\");");
                    }
                    if (Food().Contains(item.Name))
                    {
                        stacksFood += Lua.LuaDoString<int>("return GetItemCount(\"" + item.Name + "\");");
                    }
                }
                if (stacksWater < 10 && ConjureWater.IsSpellUsable && ConjureWater.KnownSpell && Bag.GetContainerNumFreeSlotsNormalType > 1)
                    ConjureWater.Launch();
                if (stacksFood < 10 && ConjureFood.IsSpellUsable && ConjureFood.KnownSpell && Bag.GetContainerNumFreeSlotsNormalType > 1)
                    ConjureFood.Launch();
            }
        }

        public void CheckIfThrowFoodAndDrinks()
        {
            if (!Fight.InFight)
            {
                _bagItems = Bag.GetBagItem();
                int bestDrink = 0;
                int bestFood = 0;
                foreach (WoWItem item in _bagItems)
                {
                    if (Drink().Contains(item.Name))
                    {
                        bestDrink = Drink().IndexOf(item.Name) > bestDrink ? Drink().IndexOf(item.Name) : bestDrink;
                    }
                    if (Food().Contains(item.Name))
                    {
                        bestFood = Food().IndexOf(item.Name) > bestFood ? Food().IndexOf(item.Name) : bestFood;
                    }
                }
                foreach (WoWItem item in _bagItems)
                {
                    if (Drink().Contains(item.Name) && Drink().IndexOf(item.Name) < bestDrink)
                    {
                        ToolBox.LuaDeleteItem(item.Name);
                    }
                    if (Food().Contains(item.Name) && Food().IndexOf(item.Name) < bestFood)
                    {
                        ToolBox.LuaDeleteItem(item.Name);
                    }
                }
            }
        }

        public void CheckIfHaveManaStone()
        {
            if (!Fight.InFight && ManaStone == "")
            {
                _bagItems = Bag.GetBagItem();
                bool haveManaStone = false;
                foreach (WoWItem item in _bagItems)
                {
                    if (ManaStones().Contains(item.Name))
                    {
                        haveManaStone = true;
                        ManaStone = item.Name;
                    }
                }

                if (!haveManaStone && Bag.GetContainerNumFreeSlotsNormalType > 1)
                {
                    if (ConjureManaEmerald.KnownSpell)
                    {
                        if (ConjureManaEmerald.IsSpellUsable)
                            ConjureManaEmerald.Launch();
                    }
                    else if (ConjureManaRuby.KnownSpell)
                    {
                        if (ConjureManaRuby.IsSpellUsable)
                            ConjureManaRuby.Launch();
                    }
                    else if (ConjureManaCitrine.KnownSpell)
                    {
                        if (ConjureManaCitrine.IsSpellUsable)
                            ConjureManaCitrine.Launch();
                    }
                    else if (ConjureManaJade.KnownSpell)
                    {
                        if (ConjureManaJade.IsSpellUsable)
                            ConjureManaJade.Launch();
                    }
                    else if (ConjureManaAgate.KnownSpell)
                    {
                        if (ConjureManaAgate.IsSpellUsable)
                            ConjureManaAgate.Launch();
                    }
                }
            }
        }

        public void UseManaStone()
        {
            ItemsManager.UseItemByNameOrId(ManaStone);
        }
    }
}