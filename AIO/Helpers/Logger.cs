using robotManager.Helpful;
using System.Drawing;
using wManager.Wow.ObjectManager;

namespace WholesomeTBCAIO.Helpers
{
    public class Logger
    {
        private static string wowClass = ObjectManager.Me.WowClass.ToString();
        private static bool _debug = false;
        private static string FCName = "Wholesome TBC AIO";

        public static void LogFight(string message)
        {
            Logging.Write($"[{FCName} - {wowClass}]: { message}", Logging.LogType.Fight, Color.ForestGreen);
        }

        public static void LogError(string message)
        {
            Logging.Write($"[{FCName} - {wowClass}]: {message}", Logging.LogType.Error, Color.DarkRed);
        }

        public static void Log(string message)
        {
            Logging.Write($"[{FCName} - {wowClass}]: {message}", Logging.LogType.Normal, Color.DarkSlateBlue);
        }

        public static void Log(string message, Color c)
        {
            Logging.Write($"[{FCName} - {wowClass}]: {message}", Logging.LogType.Normal, c);
        }
        
        public static void LogDebug(string message)
        {
            if (_debug)
                Logging.WriteDebug($"[{FCName} - {wowClass}]: { message}");
        }

        public static void CombatDebug(string message)
        {
            Logging.Write($"[{FCName} - {wowClass}]: { message}", Logging.LogType.Normal, Color.Plum);
        }
    }
}
