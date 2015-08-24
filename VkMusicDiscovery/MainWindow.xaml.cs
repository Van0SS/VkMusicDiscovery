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
using Path = System.IO.Path;

namespace VkMusicDiscovery
{
    public partial class MainWindow : Window
    {
        #region - Fields - 

        private readonly AudioFunctions audioFunctions;
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
        /// Воркер для ассинхронной загрузки файлов и отображения прогресс бара.
        /// </summary>
        private readonly BackgroundWorker _workerDownload;

        private delegate void UpdateProgressBarDelegate(DependencyProperty dp, object value);

        private delegate void ChangeTextDelegate(DependencyProperty dp, object value);

        /// <summary>
        /// Папка выбранная для загрузки.
        /// </summary>
        private string _directoryToDownload;


                //Это для текущей загрузки:
        /// <summary>
        /// Блокировать загруженные файлы
        /// </summary>
        private bool isAddDownToBlock;

        /// <summary>
        /// Заменять загружаемые файлы
        /// </summary>
        private bool isReplaceBetterKbps;
                //

        #endregion - Fields -
        //---------------------------------------------------------------------------------------------
        #region - Contructor -
        public MainWindow()
        {
            InitializeComponent();

            //Авторизация 
            WindowLogin windowLogin = new WindowLogin();
            windowLogin.Autorize();
            windowLogin.ShowDialog();
            //Если форма была закрыта, и токен не передали то закрываем программу.
            if (windowLogin.AccessToken == null)
            {
                Closing -= MainWindow_OnClosing;
                Close();
                return;
            }
            audioFunctions = new AudioFunctions(windowLogin.AccessToken, windowLogin.UserId);

            //После получения доступа задаём запрос на список рекомендуемых песен.
            _audiosRecomendedList = audioFunctions.GetRecommendations(Convert.ToInt32(TxbCount.Text));
            //Инициализация воркера
            _workerDownload = new BackgroundWorker();
            _workerDownload.WorkerSupportsCancellation = true; //Для возможности отмены.
            _workerDownload.DoWork += worker_DoWork;
            _workerDownload.RunWorkerCompleted += worker_RunWorkerCompleted;

            //Считывание настроек.
            Settings.ReadSettings();
            if (Settings.PathCurUsedArtists != "New")
                BlockCollection(Settings.PathCurUsedArtists, BlockTabType.Artists);
            if (Settings.PathCurUsedSongs != "New")
                BlockCollection(Settings.PathCurUsedSongs, BlockTabType.Songs);
            SldVolume.Value = Settings.Volume;

            PlayerInitialization();

            RbtnLangAll.IsChecked = true; //Пост установка флага, иначе вызывается событие раньше времени.
            FilterSongs(); //Фильтруем песни.
            DataGridAudio.ItemsSource = _fileteredRecomendedList; //Привязываем готовый список к датагрид.
        }
        #endregion - Constructor -
        //---------------------------------------------------------------------------------------------
        #region - Public methods -

        /// <summary>
        /// Добавить элементы из коллекции в лист блокировки.
        /// </summary>
        /// <param name="fileName">Путь</param>
        /// <param name="tabType">Тип листа</param>
        public void BlockCollection(string fileName, BlockTabType tabType)
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
                            if (indexOfSep == -1)
                                throw new Exception();
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
        /// Добавить в список блокировки по названию файла
        /// </summary>
        /// <param name="filePath">Путь до файла</param>
        /// <param name="tabType">Тип блокировки</param>
        public void BlockHeader(string filePath, BlockTabType tabType)
        {
#if !DEBUG
            try
            {
#endif
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var indexOfSep = fileName.IndexOf(" - ");
                if (indexOfSep == -1)
                    throw new Exception();
                var artist = fileName.Substring(0, indexOfSep);
                if (tabType == BlockTabType.Artists)
                {
                    BlockedArtistList.Add(new ArtistToBind(artist));
                }
                else
                {
                    var title = fileName.Substring(indexOfSep + 3);
                    BlockedSongList.Add(new ArtistTitleToBind(artist, title));
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
        #region - Private methods -
        /// <summary>
        /// Процесс асинхронной загрузки файлов.
        /// </summary>
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

                if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1) //Если есть недопустимые символы, то удалить.
                    fileName = string.Concat(fileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

                if (fileName.Length > 250) //Если имя файла длинее 250 символов - обрезать.
                    fileName = fileName.Substring(0, 250);
                Audio replaceAudio = track;
                if (isReplaceBetterKbps)
                    replaceAudio = audioFunctions.ReplaceWithBetterQuality(track);

                new WebClient().DownloadFile(replaceAudio.Url, _directoryToDownload + '\\' + fileName);
                if (isAddDownToBlock)
                    BlockHeader(fileName, BlockTabType.Songs);

                Dispatcher.Invoke(updateProgress, ProgressBar.ValueProperty, ++value);
                Dispatcher.Invoke(changeText, TextBlock.TextProperty, value + "/" + filesToDownloadList.Count);
            }
        }



        /// <summary>
        /// Если песня на неверном языке - то true.
        /// </summary>
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

        /// <summary>
        /// Проверка исполинетеля на наличие в списке блокировки
        /// </summary>
        private bool IsContentInBlockArtists(Audio track)
        {
            var curArtist = Utils.ToLowerButFirstUp(track.Artist);
            return (BlockedArtistList.Any(a => a.Artist == curArtist));
        }

        /// <summary>
        /// Проверка песни на наличие в списке блокировки
        /// </summary>
        private bool IsContentInBlockSongs(Audio track)
        {
            var blockSong = new ArtistTitleToBind(track.Artist, track.Title);
            return (BlockedSongList.Any(a => (a.Artist == blockSong.Artist) &&
                                              (a.Title == blockSong.Title)));
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

        #endregion - Private methods -
        //---------------------------------------------------------------------------------------------
        #region - Event handlers -
        
        /// <summary>
        /// Отобразить исход закачки.
        /// </summary>
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BtnDownloadall.Content = "Download All";
            ProgressBarDownload.Value = 0;
            TblProgressBar.Text = e.Cancelled ? "Canceled" : "Completed";
        }

        /// <summary>
        /// Скачивать файлы, с возможностью отмены.
        /// </summary>
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            DownloadFiles();
            if (_workerDownload.CancellationPending)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Послать запрос на сервер за новым списком.
        /// </summary>
        private void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            int count = Convert.ToInt32(TxbCount.Text);
            bool random = CbxRandom.IsChecked.Value;
            int offset = Convert.ToInt32(TxbOffset.Text);
            _audiosRecomendedList = audioFunctions.GetRecommendations(count, random, offset);
            FilterSongs();
        }

        /// <summary>
        /// Скачивать, либо остановить загрузку.
        /// </summary>
        private void BtnDownloadall_OnClick(object sender, RoutedEventArgs e)
        {
            if (_workerDownload.IsBusy)
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
                isAddDownToBlock = (bool)CbxAddToBlock.IsChecked;
                isReplaceBetterKbps = (bool) CbxBestBitrate.IsChecked;
                _workerDownload.RunWorkerAsync();
            }
        }
        /// <summary>
        /// При смене фильтра языка - сразу его применить.
        /// </summary>
        private void RbtnsLang_OnChecked(object sender, RoutedEventArgs e)
        {
            FilterSongs();
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

        /// <summary>
        /// Скопировать "исполнитель - назавние" песни.
        /// </summary>
        private void MenuItemCopeName_OnClick(object sender, RoutedEventArgs e)
        {
            var audio = (Audio)DataGridAudio.SelectedItem;
            Clipboard.SetText(audio.GetArtistDashTitle());
        }

        /// <summary>
        /// Сохранить листы блокировки и настройки при выходе.
        /// </summary>
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


        private void MenuItemDownload_OnClick_OnClick(object sender, RoutedEventArgs e)
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

                /// <summary>
        /// Выйти из аккаунта.(Но пока только открыть браузер)
        /// </summary>
        private void BtnLogOut_OnClick(object sender, RoutedEventArgs e)
        {
            WindowLogin winLogout = new WindowLogin();
            winLogout.LogOut();
            winLogout.Show();
        }

        /// <summary>
        /// Скачать выделенные песни без отображения процесса.
        /// </summary>
        private void MenuItemDownload_OnClick(object sender, RoutedEventArgs e)
        {
            var dirDialog = new CommonOpenFileDialog { IsFolderPicker = true };
            if (dirDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach (Audio audio in DataGridAudio.SelectedItems)
                {
                    new WebClient().DownloadFileAsync(audio.Url, dirDialog.FileName + '\\' + audio.GetArtistDashTitle() + ".mp3");
                }

            }
        }

        /// <summary>
        /// Добавить песни в свои аудиозаписи.
        /// </summary>
        private void MenuItemAddToAudios_OnClick(object sender, RoutedEventArgs e)
        {
            List<Audio> audios = DataGridAudio.SelectedItems.Cast<Audio>().ToList();
            audioFunctions.AddAudioToSongs(audios);
        }

        #endregion - Event handlers -


    }
}
