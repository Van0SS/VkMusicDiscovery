using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkMusicDiscovery
{
    public class StaticFunc
    {
        public static string ToLowerButFirstUp(string name)
        {
            return char.ToUpperInvariant(name[0]) + name.Substring(1).ToLowerInvariant();
        }

    }
}
