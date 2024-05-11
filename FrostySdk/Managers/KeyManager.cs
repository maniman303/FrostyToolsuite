using System.Collections.Generic;

namespace FrostySdk.Managers
{
    public class KeyManager
    {
        private Dictionary<string, byte[]> keys = new Dictionary<string, byte[]>();

        private static KeyManager _instance = null;

        public static KeyManager Instance
        { 
            get
            {
                if (_instance == null)
                {
                    _instance = new KeyManager();
                }

                return _instance;
            }
        }

        private KeyManager()
        {
        }

        public void AddKey(string id, byte[] data)
        {
            if (!keys.ContainsKey(id))
                keys.Add(id, null);
            keys[id] = data;
        }

        public byte[] GetKey(string id) => !keys.ContainsKey(id) ? null : keys[id];

        public bool HasKey(string id) => keys.ContainsKey(id);
    }
}
