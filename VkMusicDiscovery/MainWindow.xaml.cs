using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using VkMusicDiscovery.Enums;

namespace VkMusicDiscovery
{
    public partial class MainWindow : Window
    {
        #region - Fields - 

        private readonly VkApi _vkApi;
        /// <summary>
        /// Исходный список песен от сервера.
        /// </summary>
        private List<Audio> _audiosRecomendedList;
        /// <summary>
        /// Список песен для загрузки, после наложения фильтров(Язык, блокировка исп/песн).
        /// </summary>
        private readonly List<Audio> _fileteredRecomendedList = new List<Audio>();
        /// <summary>
        /// Список заблокированных исполнителей.
        /// </summary>
        public readonly List<ArtistToBind> BlockedArtistList = new List<ArtistToBind>();
        /// <summary>
        /// Список заблокированных песен.
        /// </summary>
        public readonly List<ArtistTitleToBind> BlockedSongList = new List<ArtistTitleToBind>();
        /// <summary>
        /// Воркер для ассинхронной загрузки файлов и оторжажения прогресс бара.
        /// </summary>
        private readonly BackgroundWorker _workerDownload;

        private delegate void UpdateProgressBarDelegate(DependencyProperty dp, object value);

        private delegate void ChangeTextDelegate(DependencyProperty dp, object value);

        /// <summary>
        /// Папка выбранная для загрузки.
        /// </summary>
        private string _directoryToDownload;

        #endregion - Fields -
        //---------------------------------------------------------------------------------------------
        #region - Contructor -
        public MainWindow()
        {
            InitializeComponent();

            //Авторизация 
            WindowLogin windowLogin = new WindowLogin();
            windowLogin.ShowDialog();
            //Если форма была закрыта, и токен не передали то закрываем программу.
            if (windowLogin.AccessToken == null)
            {
                Closing -= MainWindow_OnClosing;
                Close();
                return;
            }
            _vkApi = new VkApi(windowLogin.AccessToken, windowLogin.UserId);

            //После получения доступа задаём запрос на список рекомендуемых песен.
            _audiosRecomendedList = _vkApi.AudioGetRecommendations(Convert.ToInt32(TxbCount.Text));

            //Инициализация воркера
            _workerDownload = new BackgroundWorker();
            _workerDownload.WorkerSupportsCancellation = true; //Для возможности отмены.
            _workerDownload.DoWork += worker_DoWork;
            _workerDownload.RunWorkerCompleted += worker_RunWorkerCompleted;

            //Считывание настроек.
            Settings.ReadSettings();
            if (Settings.PathCurUsedArtists != "New")
                ParseFile(Settings.PathCurUsedArtists, BlockTabType.Artists);
            if (Settings.PathCurUsedSongs != "New")
                ParseFile(Settings.PathCurUsedSongs, BlockTabType.Songs);
            SldVolume.Value = Settings.Volume;

            PlayerInitialization();

            RbtnLangAll.IsChecked = true; 
            FilterSongs(); //Фильтруем песни.
            DataGridAudio.ItemsSource = _fileteredRecomendedList; //Привязываем готовый список к датагрид.
        }
        #endregion - Constructor -
        //---------------------------------------------------------------------------------------------
        #region - Public methods -

        /// <summary>
        /// Добавить элементы из файла в лист блокировки.
        /// </summary>
        /// <param name="fileName">Путь</param>
        /// <param name="tabType">Тип листа</param>
        public void ParseFile(string fileName, BlockTabType tabType)
        {
#if !DEBUG
            try
            {
#endif
                using (var fileReader = new StreamReader(fileName))
                {
                    string line;
                    while ((line = fileReader.ReadLine()) != null)
                    {
                        if (tabType == BlockTabType.Artists)
                        {
                            BlockedArtistList.Add(new ArtistToBind(line));
                        }
                        else
                        {
                            var indexOfSep = line.IndexOf(" - ");
                            var artist = line.Substring(0, indexOfSep);
                            var title = line.Substring(indexOfSep + 3);
                            BlockedSongList.Add(new ArtistTitleToBind(artist, title));
                        }
                    }
                }
#if !DEBUG
            }
            catch (Exception)
            {
                throw new Exception("fileName");
            }
#endif
        }

        /// <summary>
        /// Запись списка блокировки в файл
        /// </summary>
        /// <param name="fileFullName">Полный путь к файлу</param>
        /// <param name="tabType">Тип нужного листа</param>
        public void WriteFile(string fileFullName, BlockTabType tabType)
        {
            using (StreamWriter fileWriter = new StreamWriter(fileFullName))
            {
                if (tabType == BlockTabType.Artists)
                {
                    foreach (var artist in BlockedArtistList)
                    {
                        fileWriter.WriteLine(artist.Artist);
                    }
                }
                else
                {
                    foreach (var artist in BlockedSongList)
                    {
                        fileWriter.WriteLine(artist.Artist + " - " + artist.Title);
                    }
                }
            }
        }

        #endregion - Public methods -
        //---------------------------------------------------------------------------------------------
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BtnDownloadall.Content = "Download All";
            ProgressBarDownload.Value = 0;
            TblProgressBar.Text = e.Cancelled ? "Canceled" : "Completed";
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            DownloadFiles();
            if (_workerDownload.CancellationPending)
            {
                e.Cancel = true;
            }
        }

        private void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            int count = Convert.ToInt32(TxbCount.Text);
            bool random = CbxRandom.IsChecked.Value;
            int offset = Convert.ToInt32(TxbOffset.Text);
            _audiosRecomendedList = _vkApi.AudioGetRecommendations(count, random, offset);
            FilterSongs();
        }

        private void BtnDownloadall_OnClick(object sender, RoutedEventArgs e)
        {
            if (BtnDownloadall.Content == "Cancel")
            {
                _workerDownload.CancelAsync();

                return;
            }
            
            var dirDialog = new CommonOpenFileDialog {IsFolderPicker = true};
            if (dirDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _directoryToDownload = dirDialog.FileName;
                ProgressBarDownload.Maximum = _fileteredRecomendedList.Count;
                ProgressBarDownload.Value = 0;
                BtnDownloadall.Content = "Cancel";
                _workerDownload.RunWorkerAsync();
            }
        }

        private void DownloadFiles()
        {
            UpdateProgressBarDelegate updateProgress = ProgressBarDownload.SetValue;
            ChangeTextDelegate changeText = TblProgressBar.SetValue;
            double value = 0;
            var filesToDownloadList = new List<Audio>(_fileteredRecomendedList);
            foreach (var track in filesToDownloadList)
            {
                if (_workerDownload.CancellationPending)
                {
                    return;
                }
                string fileName = track.GetArtistDashTitle() + ".mp3"; //В вк пока только мр3.

                if (fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) != -1) //Если есть недопустимые символы, то удалить.
                    fileName = string.Concat(fileName.Split(System.IO.Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

                if (fileName.Length > 250) //Если имя файла длинее 250 символов - обрезать.
                    fileName = fileName.Substring(0, 250);

                new WebClient().DownloadFile(track.Url, _directoryToDownload + '\\' + fileName);
                Dispatcher.Invoke(updateProgress, ProgressBar.ValueProperty, ++value);
                Dispatcher.Invoke(changeText, TextBlock.TextProperty, value + "/" + filesToDownloadList.Count);
            }
        }

        private void RbtnsLang_OnChecked(object sender, RoutedEventArgs e)
        {
            FilterSongs();
        }

        /// <summary>
        /// Фильтрация текущего списка по языку и списку блокировки.
        /// </summary>
        private void FilterSongs()
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
            if (!_workerDownload.IsBusy) // Показывать количество, только если ничего не скачивается.
                TblProgressBar.Text = "Count: " + _fileteredRecomendedList.Count;
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
            var curArtist = Utils.ToLowerButFirstUp(track.Artist);
            return (BlockedArtistList.Any(a => a.Artist == curArtist));
        }

        private bool IsContentInBlockSongs(Audio track)
        {
            var blockSong = new ArtistTitleToBind(track.Artist, track.Title);
            return (BlockedSongList.Any(a => (a.Artist == blockSong.Artist) &&
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
                blockArtist = Utils.ToLowerButFirstUp(blockArtist);
                if (BlockedArtistList.All(a => a.Artist != blockArtist))
                    BlockedArtistList.Add(new ArtistToBind(blockArtist));
            }
            FilterSongs();
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
                blockSong.Artist = Utils.ToLowerButFirstUp(blockSong.Artist);
                blockSong.Title = Utils.ToLowerButFirstUp(blockSong.Title);
                if (BlockedSongList.All(a => ((a.Artist != blockSong.Artist) ||
                                              (a.Title != blockSong.Title))
                    ))
                {
                    BlockedSongList.Add(new ArtistTitleToBind(blockSong.Artist, blockSong.Title));
                }
            }
            FilterSongs();
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
            WindowBlockList windowBlockList = new WindowBlockList();
            windowBlockList.Owner = this;
#if !DEBUG
            try
            {
#endif
                windowBlockList.ShowDialog();
#if !DEBUG
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
#endif
            FilterSongs();
        }

        private void MenuItemCopeName_OnClick(object sender, RoutedEventArgs e)
        {
            var audio = (Audio)DataGridAudio.SelectedItem;
            Clipboard.SetText(audio.GetArtistDashTitle());
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (Settings.PathCurUsedArtists == "New")
                Settings.PathCurUsedArtists = Directory.GetCurrentDirectory() + "\\DefArtists.avk";

            WriteFile(Settings.PathCurUsedArtists, BlockTabType.Artists); //Пока срёт, где нахдится.


            if (Settings.PathCurUsedSongs == "New")
                Settings.PathCurUsedSongs = Directory.GetCurrentDirectory() + "\\DefSongs.avk";

            WriteFile(Settings.PathCurUsedSongs, BlockTabType.Songs); //Пока срёт, где нахдится.

            Settings.Volume = SldVolume.Value;
            Settings.WriteSettings();
        }

        //Temp
        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var dirDialog = new CommonOpenFileDialog {IsFolderPicker = true};
            if (dirDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach (Audio audio in DataGridAudio.SelectedItems)
                {
                    new WebClient().DownloadFileAsync(audio.Url, dirDialog.FileName + '\\' + audio.GetArtistDashTitle() + ".mp3");
                }
                
            }
        }
    }
}
