using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkMusicDiscovery.Enums;

namespace VkMusicDiscovery
{
    /// <summary>
    /// Для привязки списка заблокированных авторов через DataGrid в окне WindowBlockList
    /// </summary>
    public class ArtistToBind : IComparable<ArtistToBind>
    {
        private string _artist;
        public string Artist
        {
            get { return _artist; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    _artist = "Va";
                else
                {
                    _artist = Utils.ReplaceDash(value);
                    _artist = Utils.ToLowerButFirstUp(value);
                }

            }
        }

        public ArtistToBind(string artist)
        {
            Artist = artist;
        }
        public ArtistToBind() //Для возможности добавлять новые сторки через DataGrid
        { }

        public int CompareTo(ArtistToBind artist)
        {
            return String.Compare(this.Artist, artist.Artist, StringComparison.InvariantCultureIgnoreCase);
        }
    }
    /// <summary>
    /// Для привязки списка заблокированных песен через DataGrid в окне WindowBlockList
    /// </summary>
    public class ArtistTitleToBind : ArtistToBind
    {
        private string _title;

        public string Title
        {
            get { return _title; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    _title = "Track 1"; //Самый известный трек.
                else
                {
                    _title = Utils.ReplaceDash(value);
                    _title = Utils.ToLowerButFirstUp(value);
                }
            }
        }

        public ArtistTitleToBind(string artist, string title)
        {
            Artist = artist;
            Title = title;
        }
        public ArtistTitleToBind() //Для возможности добавлять новые сторки через DataGrid
        { }
    }

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
                    _artist = Utils.ReplaceDash(value);
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
                    _title = Utils.ReplaceDash(value);
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
        public AudioGenre? GenreId { get; set; }

        public string GetArtistDashTitle()
        {
            return Artist + " - " + Title;
        }
    }
}
