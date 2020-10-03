using System;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.IO;
using WholesomeTBCAIO.Helpers;

namespace WholesomeTBCAIO
{
    [Serializable]
    public class AIOTBCSettings : robotManager.Helpful.Settings
    {
        public static AIOTBCSettings CurrentSetting { get; set; }

        private AIOTBCSettings()
        {
            LastUpdateDate = 0;
        }

        public double LastUpdateDate { get; set; }

        public bool Save()
        {
            try
            {
                return Save(AdviserFilePathAndName("AIOTBCSettings",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
            }
            catch (Exception e)
            {
                Logger.LogError("AIOTBCSettings > Save(): " + e);
                return false;
            }
        }

        public static bool Load()
        {
            try
            {
                if (File.Exists(AdviserFilePathAndName("AIOTBCSettings",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName)))
                {
                    CurrentSetting = Load<AIOTBCSettings>(
                        AdviserFilePathAndName("AIOTBCSettings",
                        ObjectManager.Me.Name + "." + Usefuls.RealmName));
                    return true;
                }
                CurrentSetting = new AIOTBCSettings();
            }
            catch (Exception e)
            {
                Logger.LogError("AIOTBCSettings > Load(): " + e);
            }
            return false;
        }
    }
}