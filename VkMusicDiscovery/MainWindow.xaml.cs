﻿using System;
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
           public string Artist { get; set; }

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

        private VkApi _vkApi;
        private List<Audio> _audiosRecomendedList;
        private List<Audio> _fileteredRecomendedList = new List<Audio>();
        private List<ArtistToBind> _blockedArtistList = new List<ArtistToBind>();
        private List<ArtistTitleToBind> _blockedSongList = new List<ArtistTitleToBind>();
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
                if (_blockedArtistList.All(a => a.Artist != blockArtist))
                    _blockedArtistList.Add(new ArtistToBind(blockArtist));
            }
            BlockArtists();
            DataGridAudio.Items.Refresh();
        }

        /// <summary>
        /// Блокирование АРТИСТА по списку.
        /// </summary>
        private void BlockArtists()
        {
            for (int i = _fileteredRecomendedList.Count -1; i >= 0; i--)
            {
                var curArtist = StaticFunc.ToLowerButFirstUp(_fileteredRecomendedList[i].Artist);
                if (_blockedArtistList.Any(a => a.Artist == curArtist))
                {
                    _fileteredRecomendedList.RemoveAt(i);
                }
            }
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
                if (_blockedSongList.All(a => ((a.Artist != blockSong.Artist) ||
                                              (a.Title != blockSong.Title))
                    ))
                {
                    _blockedSongList.Add(new ArtistTitleToBind(blockSong.Artist, blockSong.Title));
                }
            }
            BlockSongs();
            DataGridAudio.Items.Refresh();
        }

        /// <summary>
        /// Блокирование ПЕСНИ по списку.
        /// </summary>
        private void BlockSongs()
        {
            for (int i = _fileteredRecomendedList.Count - 1; i >= 0; i--)
            {
                var blockSong = new ArtistTitleToBind(_fileteredRecomendedList[i].Artist, _fileteredRecomendedList[i].Title);
                if (_blockedSongList.Any(a => (a.Artist == blockSong.Artist) &&
                    (a.Title == blockSong.Title)))
                {
                    _fileteredRecomendedList.RemoveAt(i);
                }
            }
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
           // DataGridAudio.Items.Refresh();
        }
    }
}
