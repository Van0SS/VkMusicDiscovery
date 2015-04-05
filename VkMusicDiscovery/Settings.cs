using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkMusicDiscovery
{
    public static class Settings
    {
        public static string PathCurUsedArtists { get; set; }
        public static string PathCurUsedSongs { get; set; }
        public static double Volume { get; set; }

        private static readonly string SettingsPath = Directory.GetCurrentDirectory() + "\\VkMusicDiscoverySettings.settings";
        public static void ReadSettings()
        {
            if (!File.Exists(SettingsPath))
            {
                PathCurUsedArtists = "New";
                PathCurUsedSongs = "New";
                Volume = 0.7;
                return;
            }
                
            using (var streamReader = new StreamReader(SettingsPath))
            {
                if(!File.Exists(PathCurUsedArtists = streamReader.ReadLine()))
                    PathCurUsedArtists = "New";
                if (!File.Exists(PathCurUsedSongs = streamReader.ReadLine()))
                    PathCurUsedSongs = "New";
                double result;
                if (Double.TryParse(streamReader.ReadLine(), out result))
                    Volume = result;
                else
                {
                    Volume = 0.7;
                }
            }
        }

        public static void WriteSettings()
        {
            using (var streamWriter = new StreamWriter(SettingsPath))
            {
                streamWriter.WriteLine(PathCurUsedArtists);
                streamWriter.WriteLine(PathCurUsedSongs);
                streamWriter.WriteLine(Volume);
                streamWriter.WriteLine("#Note: please don't break strusture!");
            }
        }
    }

}
