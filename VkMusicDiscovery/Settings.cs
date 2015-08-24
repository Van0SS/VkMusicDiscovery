using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkMusicDiscovery
{
    /// <summary>
    /// Настройки приложения.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// Текущий список блокированных исполнителей.
        /// </summary>
        public static string PathCurUsedArtists { get; set; }

        /// <summary>
        /// Текущий список блокированных песен.
        /// </summary>
        public static string PathCurUsedSongs { get; set; }

        /// <summary>
        /// Значение громкости плеера (0-1)
        /// </summary>
        public static double Volume { get; set; }

        /// <summary>
        /// Путь до файла с настройками.
        /// </summary>
        private static readonly string SettingsPath = Directory.GetCurrentDirectory() + "\\VkMusicDiscoverySettings.settings";

        /// <summary>
        /// Считать настройки.
        /// </summary>
        public static void ReadSettings()
        {
            //Если файла настроек не обнаружено - создать новые
            if (!File.Exists(SettingsPath))
            {
                PathCurUsedArtists = "New";
                PathCurUsedSongs = "New";
                Volume = 0.7;
                return;
            }
            //Считать настройки, если что-то не найдено - поставить дефолтные.
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

        /// <summary>
        /// Записать настройки.
        /// </summary>
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
