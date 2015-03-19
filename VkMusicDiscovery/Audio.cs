using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkMusicDiscovery.Enums;

namespace VkMusicDiscovery
{
    class Audio
    {
        public uint Id { get; set; }
        /// <summary>
        /// Идентификатор владельца аудиозаписи.
        /// </summary>
        public int OwnerId { get; set; }
        public string Artist { get; set; }
        public string Title { get; set; }
        public uint Duration { get; set; }
        public Uri Url { get; set; }
        /// <summary>
        /// Идентификатор текста аудиозаписи (если доступно).
        /// </summary>
        public uint? LyricsId { get; set; }
        /// <summary>
        /// Идентификатор альбома, в котором находится аудиозапись (если присвоен).
        /// </summary>
        public AudioGenres? GenreId { get; set; }

    }
}
