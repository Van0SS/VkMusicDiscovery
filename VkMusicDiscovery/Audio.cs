using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string Url { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public uint LyricsId { get; set; }
        /// <summary>
        /// Идентификатор альбома, в котором находится аудиозапись (если присвоен).
        /// </summary>
        public uint AlbumId { get; set; }
        public uint GenreId { get; set; }

    }
}
