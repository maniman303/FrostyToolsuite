using System.Linq;

namespace Frosty.Core
{
    public static class StringHelper
    {
        public static bool ContainsWhiteSpace(this string str)
        {
            return str.ToCharArray().Any(x => char.IsWhiteSpace(x));
        }
    }
}
