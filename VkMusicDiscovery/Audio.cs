using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkMusicDiscovery.Enums;

namespace VkMusicDiscovery
{
    public class Audio
    {
        private string _artist;
        private string _title;
        public uint Id { get; set; }
        /// <summary>
        /// Идентификатор владельца аудиозаписи.
        /// </summary>
        public int OwnerId { get; set; }
        public string Artist
        {
            get { return _artist; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    _artist = "VA"; //Хороший исполнитель.
                else
                {
                    _artist = value;
                }
            }
        }
        public string Title
        {
            get { return _title; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    _title = "Track 1"; //Самый известный трек.
                else
                {
                    _title = value;
                }
            }
        }
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
