using System;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.IO;
using WholesomeTBCAIO.Helpers;

namespace WholesomeTBCAIO
{
    [Serializable]
    public class FCSettings : robotManager.Helpful.Settings
    {
        public static FCSettings CurrentSetting { get; set; }

        private FCSettings()
        {
            LastUpdateDate = 0;
        }

        public double LastUpdateDate { get; set; }

        public bool Save()
        {
            try
            {
                return Save(AdviserFilePathAndName("ZETBCFCSettings",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
            }
            catch (Exception e)
            {
                Logger.LogError("ZETBCFCSettings > Save(): " + e);
                return false;
            }
        }

        public static bool Load()
        {
            try
            {
                if (File.Exists(AdviserFilePathAndName("ZETBCFCSettings",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName)))
                {
                    CurrentSetting = Load<FCSettings>(
                        AdviserFilePathAndName("ZETBCFCSettings",
                        ObjectManager.Me.Name + "." + Usefuls.RealmName));
                    return true;
                }
                CurrentSetting = new FCSettings();
            }
            catch (Exception e)
            {
                Logger.LogError("ZETBCFCSettings > Load(): " + e);
            }
            return false;
        }
    }
}