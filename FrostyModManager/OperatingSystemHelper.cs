using System.Runtime.InteropServices;

namespace FrostyModManager
{
    public static class OperatingSystemHelper
    {
        private static bool? _value = null;

        public static bool IsWine()
        {
            if (_value == null)
            {
                try
                {
                    var version = GetWineVersion();
                    _value = true;
                }
                catch
                {
                    _value = false;
                }
            }

            return _value ?? false;
        }

        [DllImport("ntdll.dll", EntryPoint = "wine_get_version", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern string GetWineVersion();
    }
}
