using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkMusicDiscovery
{
    public class Utils
    {
        /// <summary>
        /// Сделать нижний регистр, но первая буква в верхнем.
        /// </summary>
        public static string ToLowerButFirstUp(string name)
        {
            return char.ToUpperInvariant(name[0]) + name.Substring(1).ToLowerInvariant();
        }
    }
}
