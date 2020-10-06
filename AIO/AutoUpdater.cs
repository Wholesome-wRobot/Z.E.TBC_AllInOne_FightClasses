using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Net;
using System.Text;
using System.Threading;
using WholesomeTBCAIO;
using WholesomeTBCAIO.Helpers;
using wManager.Wow.Helpers;

    public static class AutoUpdater
    {
        public static void CheckUpdate(string MyCurrentVersion)
        {
            if (wManager.Information.Version.Contains("1.7.2"))
            {
                Logger.Log($"AIO couldn't load (v {wManager.Information.Version})");
                Products.ProductStop();
                return;
            }

            DateTime dateBegin = new DateTime(2020, 1, 1);
            DateTime currentDate = DateTime.Now;

            long elapsedTicks = currentDate.Ticks - dateBegin.Ticks;
            elapsedTicks /= 10000000;

            double timeSinceLastUpdate = elapsedTicks - AIOTBCSettings.CurrentSetting.LastUpdateDate;

            // If last update try was < 10 seconds ago, we exit to avoid looping
            if (timeSinceLastUpdate < 10)
            {
                Logger.Log($"Update failed {timeSinceLastUpdate} seconds ago. Exiting updater.");
                return;
            }

            try
            {
                AIOTBCSettings.CurrentSetting.LastUpdateDate = elapsedTicks;
                AIOTBCSettings.CurrentSetting.Save();

                Logger.Log("Starting updater");
                string onlineFile = "https://github.com/Wholesome-wRobot/Wholesome-TBC-AIO-Fightclasses/raw/master/AIO/Compiled/Wholesome_TBC_AIO_Fightclasses.dll";

                // Version check
                string onlineVersion = "https://raw.githubusercontent.com/Wholesome-wRobot/Wholesome-TBC-AIO-Fightclasses/master/AIO/Compiled/Version.txt";
                var onlineVersionContent = new WebClient { Encoding = Encoding.UTF8 }.DownloadString(onlineVersion);
                if (onlineVersionContent == null || onlineVersionContent.Length > 10 || onlineVersionContent == MyCurrentVersion)
                {
                    Logger.Log($"Your version is up to date ({MyCurrentVersion})"); 
                    return;
                }

                // File check
                string currentFile = Others.GetCurrentDirectory + @"\FightClass\" + wManager.wManagerSetting.CurrentSetting.CustomClass;
                var onlineFileContent = new WebClient { Encoding = Encoding.UTF8 }.DownloadData(onlineFile);
                if (onlineFileContent != null && onlineFileContent.Length > 0)
                {
                    Logger.Log($"Your version : {MyCurrentVersion}");
                    Logger.Log($"Online Version : {onlineVersionContent}");
                    Logger.Log("Trying to update");
                    System.IO.File.WriteAllBytes(currentFile, onlineFileContent); // replace user file by online file
                    Thread.Sleep(5000);
                    new Thread(CustomClass.ResetCustomClass).Start();
                }
            }
            catch (Exception e)
            {
                Logging.WriteError("Auto update: " + e);
            }
        }
    }
