using wManager.Wow.ObjectManager;

namespace WholesomeTBCAIO.Helpers
{
    public class RangeManager
    {
        public static readonly float DefaultMeleeRange = 5f;
        private static float _settingRange = DefaultMeleeRange;

        public static void SetRange(float range)
        {
            if (range != _settingRange)
            {
                _settingRange = range;
                Logger.Log($"Range set to {_settingRange}");
            }
        }

        public static void SetRangeToMelee()
        {
            if (ObjectManager.Target != null)
                SetRange(DefaultMeleeRange + (ObjectManager.Target.CombatReach / 2.5f));
            else
                SetRange(DefaultMeleeRange);
        }

        public static bool CurrentRangeIsMelee()
        {
            if (ObjectManager.Target != null)
                return (decimal)GetRange() == (decimal)(DefaultMeleeRange + (ObjectManager.Target.CombatReach / 2.5f));
            else
                return (decimal)GetRange() == (decimal)DefaultMeleeRange;
        }

        public static float GetRange()
        {
            return _settingRange;
        }
    }
}
