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
        private int _kbps;
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

        public int Kbps
        {
            get { return _kbps; }
            set
            {
               // if (value >= 320)
                 //   _kbps = 320;
                if (value <= 0)
                {
                    throw new Exception("Value must be above 0");
                }
                else
                {
                    _kbps = value; //Пока мб >320 ибо с картинками.
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
