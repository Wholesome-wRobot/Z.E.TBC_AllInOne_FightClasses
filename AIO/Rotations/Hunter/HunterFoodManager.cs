using System.Collections.Generic;
using wManager.Wow.Helpers;
using System.Threading;

namespace WholesomeTBCAIO.Rotations.Hunter
{
    public class HunterFoodManager
    {
        private string PetFoodType()
        {
            return Lua.LuaDoString<string>("return GetPetFoodTypes();", "");
        }

        private List<string> FoodList()
        {
            return new List<string>
        {
            "Tough Jerky",
            "Haunch of Meat",
            "Mutton Chop",
            "Wild Hog Shank",
            "Cured Ham Steak",
            "Roasted Quail",
            "Smoked Talbuk Venison",
            "Clefthoof Ribs",
            "Salted Venison",
            "Mead Basted Caribou",
            "Mystery Meat",
            "R﻿ed Wolf Mea﻿﻿t"
        };
        }

        private List<string> Fungus()
        {
            return new List<string>
        {
            "Raw Black Truffle"
        };
        }

        private List<string> FishList()
        {
            return new List<string>
        {
            "Slitherskin Mackerel",
            "Longjaw Mud Snapper",
            "Bristle Whisker Catfish",
            "Rockscale Cod",
            "Striped Yellowtail",
            "Spinefin Halibut",
            "Sunspring Carp",
            "Zangar Trout",
            "Fillet of Icefin",
            "Poached Emperor Salmon"
        };
        }

        private List<string> FruitList()
        {
            return new List<string>
        {
            "Shiny Red Apple",
            "Tel'Abim Banana",
            "Snapvine Watermelon",
            "Goldenbark Apple",
            "Heaven Peach",
            "Moon Harvest Pumpkin",
            "Deep Fried Plantains",
            "Skethyl Berries",
            "Telaari Grapes",
            "Tundra Berries",
            "Savory Snowplum"
        };
        }

        private List<string> BreadList()
        {
            return new List<string>
        {
            "Tough Hunk of Bread",
            "Freshly Baked Bread",
            "Moist Cornbread",
            "Mulgore Spice Bread",
            "Soft Banana Bread",
            "Homemade Cherry Pie",
            "Mag'har Grainbread",
            "Crusty Flatbread",
            "Bladespire Bagel"
        };
        }

        private void FeedByType(List<string> list)
        {
            foreach (string text in list)
            {
                if (ItemsManager.GetItemCountByNameLUA(text) > 0)
                {
                    Lua.LuaDoString("CastSpellByName('Feed Pet')", false);
                    Lua.LuaDoString("UseItemByName('" + text + "')", false);
                    Thread.Sleep(5000);
                }
            }
        }

        public void FeedPet()
        {
            if (PetFoodType().Contains("Meat"))
            {
                FeedByType(FoodList());
            }
            if (PetFoodType().Contains("Fungus"))
            {
                FeedByType(Fungus());
            }
            if (PetFoodType().Contains("Fish"))
            {
                FeedByType(FishList());
            }
            if (PetFoodType().Contains("Fruit"))
            {
                FeedByType(FruitList());
            }
            if (PetFoodType().Contains("Bread"))
            {
                FeedByType(BreadList());
            }
        }
    }
}