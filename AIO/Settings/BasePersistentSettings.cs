using System.IO;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeTBCAIO.Settings
{
    public abstract class BasePersistentSettings<T> : BaseSettings where T : BasePersistentSettings<T>, new()
    {
        private static string FileName => AdviserFilePathAndName(typeof(T).Name, ObjectManager.Me.Name + "." + Usefuls.RealmName);

        protected override void OnUpdate()
        {
            Save(FileName);
        }

        private static T _current;
        public static T Current
        {
            get
            {
                if (_current == null)
                {
                    var fileName = FileName;
                    _current = File.Exists(fileName) ? Load<T>(fileName) : new T();
                    _current.OnUpdate();
                }

                return _current;
            }
        }
    }
}
