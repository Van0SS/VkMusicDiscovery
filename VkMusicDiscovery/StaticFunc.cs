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

        public static string PathBlockArtists
        {
            get { return Properties.Settings.Default.PathCurUsedArtists; }
            set
            {
                if ((value != "New") && (!File.Exists(value)))
                    throw new Exception("File not New, or not exist");
                Properties.Settings.Default.PathCurUsedArtists = value;
            }
        }

        public static string PathBlockSongs
        {
            get { return Properties.Settings.Default.PathCurUsedSongs; }
            set
            {
                if ((value != "New") && (!File.Exists(value)))
                    throw new Exception("File not New, or not exist");
                Properties.Settings.Default.PathCurUsedSongs = value;
            }
        }

        public static string ToLowerButFirstUp(string name)
        {
            return char.ToUpperInvariant(name[0]) + name.Substring(1).ToLowerInvariant();
        }

        //Если после конкатенации названия с добавлением " - " будет ещё такая же конструкция
        //то будут проблемы с парсингом, да и просто не красиво.
        public static string ReplaceDash(string name)
        {
            name = name.Replace(" -", " ");
            name = name.Replace("- ", " ");
            return name;
        }
    }
}
