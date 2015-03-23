using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using VkMusicDiscovery.Enums;

namespace VkMusicDiscovery
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Для привязки списка заблокированных авторов через DataGrid в окне WindowBlockList
        /// </summary>
        public class ArtistToBind
        {
            private string _artist;
            public string Artist
            {
                get { return _artist; }
                set
                {
                    if (String.IsNullOrEmpty(value))
                        _artist = "VA";
                    else
                    {
                        _artist = value;
                    }

                }
            }

            public ArtistToBind(string artist)
            {
                Artist = artist;
            }
            public ArtistToBind() //Для возможности добавлять новые сторки через DataGrid
            { }
        }
        /// <summary>
        /// Для привязки списка заблокированных песен через DataGrid в окне WindowBlockList
        /// </summary>
        public class ArtistTitleToBind
        {
            public string Artist { get; set; }
            public string Title { get; set; }

            public ArtistTitleToBind(string artist, string title)
            {
                Artist = artist;
                Title = title;
            }
            public ArtistTitleToBind() //Для возможности добавлять новые сторки через DataGrid
            { }
        }

        private readonly VkApi _vkApi;
        private List<Audio> _audiosRecomendedList;
        private readonly List<Audio> _fileteredRecomendedList = new List<Audio>();
        private readonly List<ArtistToBind> _blockedArtistList = new List<ArtistToBind>();
        private readonly List<ArtistTitleToBind> _blockedSongList = new List<ArtistTitleToBind>();
        public MainWindow()
        {
            InitializeComponent();
            WindowLogin windowLogin = new WindowLogin();
            windowLogin.ShowDialog();
            _vkApi = new VkApi(windowLogin.AccessToken, windowLogin.UserId);
            _audiosRecomendedList = _vkApi.AudioGetRecommendations(10, true);

            _fileteredRecomendedList.AddRange(_audiosRecomendedList);
            DataGridAudio.ItemsSource = _fileteredRecomendedList;

            RbtnLangAll.IsChecked = true;
        }

        private void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            int count = Convert.ToInt32(TxbCount.Text);
            bool random = CbxRandom.IsChecked.Value;
            int offset = Convert.ToInt32(TxbOffset.Text);
            _audiosRecomendedList = _vkApi.AudioGetRecommendations(count, random, offset);
            FilterAndBindData();
        }

        private void BtnDownloadall_OnClick(object sender, RoutedEventArgs e)
        {
            var dirDialog = new CommonOpenFileDialog {IsFolderPicker = true};
            if (dirDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {

                foreach (var track in _fileteredRecomendedList)
                {
                    string fileName = track.Artist + " - " + track.Title + ".mp3"; //В вк пока только мр3.
                    
                    if (fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) != -1) //Если есть недопустимые символы, то удалить.
                        fileName = string.Concat(fileName.Split(System.IO.Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                    
                    if (fileName.Length > 250) //Если имя файла длинее 250 символов - обрезать.
                        fileName = fileName.Substring(0, 250);
                    
                    new WebClient().DownloadFileAsync(track.Url, dirDialog.FileName +
                        '\\' + fileName);
                }
            }
        }

        private void RbtnsLang_OnChecked(object sender, RoutedEventArgs e)
        {
            FilterAndBindData();
        }

        private void FilterAndBindData()
        {
            _fileteredRecomendedList.Clear();
            foreach (var track in _audiosRecomendedList)
            {
                if (FailCurLang(track))
                    continue;

                if (IsContentInBlockArtists(track))
                    continue;

                if (IsContentInBlockSongs(track))
                    continue;

                _fileteredRecomendedList.Add(track);
            }
            DataGridAudio.Items.Refresh();
        }

        private bool FailCurLang(Audio track)
        {
            if (RbtnLangRu.IsChecked == true)
            {
                if (!Regex.IsMatch((track.Artist + track.Title), "[А-Яа-я]"))
                    return true;
            }
            if (RbtnLangEng.IsChecked == true)
            {
                if (Regex.IsMatch((track.Artist + track.Title), "[А-Яа-я]"))
                    return true;
            }
            return false;

        }

        private bool IsContentInBlockArtists(Audio track)
        {
            var curArtist = StaticFunc.ToLowerButFirstUp(track.Artist);
            return (_blockedArtistList.Any(a => a.Artist == curArtist));
        }

        private bool IsContentInBlockSongs(Audio track)
        {
            var blockSong = new ArtistTitleToBind(track.Artist, track.Title);
            return (_blockedSongList.Any(a => (a.Artist == blockSong.Artist) &&
                                              (a.Title == blockSong.Title)));
        }
        /// <summary>
        /// Добавление АРТИСТА в лист блокировки.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItemBlockArtist_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in DataGridAudio.SelectedItems)
            {
                var blockArtist = ((Audio) item).Artist;
                blockArtist = StaticFunc.ToLowerButFirstUp(blockArtist);
                if (_blockedArtistList.All(a => a.Artist != blockArtist))
                    _blockedArtistList.Add(new ArtistToBind(blockArtist));
            }
            FilterAndBindData();
            DataGridAudio.Items.Refresh();
        }

        /// <summary>
        /// Добавление ПЕСНИ в лист блокировки.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItemBlockSong_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in DataGridAudio.SelectedItems)
            {
                var blockSong = (Audio) item;
                blockSong.Artist = StaticFunc.ToLowerButFirstUp(blockSong.Artist);
                blockSong.Title = StaticFunc.ToLowerButFirstUp(blockSong.Title);
                if (_blockedSongList.All(a => ((a.Artist != blockSong.Artist) ||
                                              (a.Title != blockSong.Title))
                    ))
                {
                    _blockedSongList.Add(new ArtistTitleToBind(blockSong.Artist, blockSong.Title));
                }
            }
            FilterAndBindData();
            DataGridAudio.Items.Refresh();
        }

        /// <summary>
        /// Проверка ввода для полей с числами.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxbCountOffset_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = "0123456789".IndexOf(e.Text) < 0;
        }
        /// <summary>
        /// Вызов формы для редактирования списка заблокированных.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnEditBlocked_OnClick(object sender, RoutedEventArgs e)
        {
            WindowBlockList windowBlockList = new WindowBlockList(_blockedArtistList, _blockedSongList);
            windowBlockList.ShowDialog();
            FilterAndBindData();
        }
    }
}
