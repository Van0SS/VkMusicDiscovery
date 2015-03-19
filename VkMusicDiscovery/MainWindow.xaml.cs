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
        private VkApi _vkApi;
        private List<Audio> _audiosRecomendedList;
        private List<Audio> _fileteredRecomendedList = new List<Audio>();
        private List<string> _blockedArtistList = new List<string>();
        private List<string> _blockedSongList = new List<string>();
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
            FilterByLangAndBindData();
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
            FilterByLangAndBindData();
        }

        private void FilterByLangAndBindData()
        {
            _fileteredRecomendedList.Clear();
            if (RbtnLangRu.IsChecked == true)
            {
                foreach (var track in _audiosRecomendedList)
                {
                    if (Regex.IsMatch((track.Artist + track.Title), "[А-Яа-я]"))
                        _fileteredRecomendedList.Add(track);
                }
            }
            else if (RbtnLangEng.IsChecked == true)
            {
                foreach (var track in _audiosRecomendedList)
                {
                    if (!Regex.IsMatch((track.Artist + track.Title), "[А-Яа-я]"))
                        _fileteredRecomendedList.Add(track);
                }
            }
            else
            {
                _fileteredRecomendedList.AddRange(_audiosRecomendedList);
            }
            BlockArtists();
            BlockSongs();
            DataGridAudio.Items.Refresh();
        }

        private void MenuItemBlockArtist_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in DataGridAudio.SelectedItems)
            {
                var artist = ((Audio) item).Artist.ToLowerInvariant();
                if (!_blockedArtistList.Contains(artist))
                    _blockedArtistList.Add(artist);
            }
            BlockArtists();
            DataGridAudio.Items.Refresh();
        }

        private void BlockArtists()
        {
            for (int i = _fileteredRecomendedList.Count -1; i >= 0; i--)
            {
                if (_blockedArtistList.Contains(_fileteredRecomendedList[i].Artist.ToLowerInvariant()))
                {
                    _fileteredRecomendedList.RemoveAt(i);
                }
            }
        }

        private void MenuItemBlockSong_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in DataGridAudio.SelectedItems)
            {
                var track = (Audio) item;
                var artTitle = ToArtistTitleLower(track);
                if (!_blockedArtistList.Contains(artTitle))
                    _blockedSongList.Add(artTitle);
            }
            BlockSongs();
            DataGridAudio.Items.Refresh();
        }

        private static string ToArtistTitleLower(Audio track)
        {
            return track.Artist.ToLowerInvariant() + " - " + track.Title.ToLowerInvariant();
        }

        private void BlockSongs()
        {
            for (int i = _fileteredRecomendedList.Count - 1; i >= 0; i--)
            {
                if (_blockedSongList.Contains(ToArtistTitleLower(_fileteredRecomendedList[i])))
                {
                    _fileteredRecomendedList.RemoveAt(i);
                }
            }
        }

        /*private void TxbCount_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (Char.IsDigit(e.))
        }*/

        private void TxbCountOffset_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = "0123456789".IndexOf(e.Text) < 0;
        }
    }
}
