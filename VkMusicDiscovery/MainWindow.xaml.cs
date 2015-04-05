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

    public class ViewModel
    {
        public CollectionViewSource ViewSource { get; set; }
        public ObservableCollection<Audio> Collection { get; set; }
        public ViewModel()
        {
            this.Collection = new ObservableCollection<Audio>();
            this.ViewSource = new CollectionViewSource();
            ViewSource.Source = this.Collection;
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private readonly VkApi _vkApi;
        private List<Audio> _audiosRecomendedList;
        private readonly List<Audio> _fileteredRecomendedList = new List<Audio>();

        public readonly List<ArtistToBind> BlockedArtistList = new List<ArtistToBind>();
        
        public readonly List<ArtistTitleToBind> BlockedSongList = new List<ArtistTitleToBind>();
        private readonly BackgroundWorker _workerDownload;

        private delegate void UpdateProgressBarDelegate(DependencyProperty dp, object value);

        private delegate void ChangeTextDelegate(DependencyProperty dp, object value);

        private string _directoryToDownload;
        private MediaPlayer _mediaPlayer = new MediaPlayer();
        private MediaState _playerState;
        private bool _playerRepeatSong = false;
        private bool _playerShuffle = false;
        private DispatcherTimer timer;
        Random rnd = new Random();
        private MediaState PlayerState
        {
            get { return _playerState; }
            set
            {
                _playerState = value;
                ChangePlayButtonState();
            }
        }

        private void ChangePlayButtonState()
        {
            BtnPlayerPlayPause.Content = 
                FindResource(_playerState == MediaState.Play ? "PausePic" : "PlayPic");
        }

        public MainWindow()
        {
            InitializeComponent();
            WindowLogin windowLogin = new WindowLogin();
            windowLogin.ShowDialog();
            _vkApi = new VkApi(windowLogin.AccessToken, windowLogin.UserId);
            _audiosRecomendedList = _vkApi.AudioGetRecommendations(10, true);

            _fileteredRecomendedList.AddRange(_audiosRecomendedList);
            DataGridAudio.ItemsSource = _fileteredRecomendedList;

            _workerDownload = new BackgroundWorker();
            _workerDownload.WorkerSupportsCancellation = true;
            _workerDownload.DoWork += worker_DoWork;
            _workerDownload.RunWorkerCompleted += worker_RunWorkerCompleted;

            RbtnLangAll.IsChecked = true;

            Settings.ReadSettings();
            if (Settings.PathCurUsedArtists != "New")
            {
                ParseFile(Settings.PathCurUsedArtists, BlockTabType.Artists);
            }
            if (Settings.PathCurUsedSongs != "New")
            {
                ParseFile(Settings.PathCurUsedSongs, BlockTabType.Songs);
            }

            FilterAndBindData();

            _mediaPlayer.MediaEnded += MediaPlayerEnded;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;

            SldVolume.Value = Settings.Volume;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            UpdateProgressBarNTime();
        }

        private void UpdateProgressBarNTime()
        {
            ProgressBarPlayer.Value = _mediaPlayer.Position.TotalSeconds;
            if (!_mediaPlayer.NaturalDuration.HasTimeSpan)
                return;
            var elapsedTime = _mediaPlayer.NaturalDuration.TimeSpan - _mediaPlayer.Position;
            String emptyZero = "";
            if (elapsedTime.Seconds < 10)
                emptyZero = "0";
            TbPlayerTime.Text = "-" + elapsedTime.Minutes + ":" + emptyZero + elapsedTime.Seconds;
        }

        /// <summary>
        /// Добавить элементы из файлы в лист блокировки.
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

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BtnDownloadall.Content = "Download All";
            ProgressBarDownload.Value = 0;
            if (e.Cancelled)
            {
                TblProgressBar.Text = "Canceled";
            }
            else
            {
                TblProgressBar.Text = "Completed";
            }
            

            //Сделать чтобы через 3 сек снова писалось кол-во.
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
            FilterAndBindData();
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
            FilterAndBindData();
        }

        /// <summary>
        /// Фильтрация текущего списка по языку и списку блокировки.
        /// </summary>
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
                blockSong.Artist = Utils.ToLowerButFirstUp(blockSong.Artist);
                blockSong.Title = Utils.ToLowerButFirstUp(blockSong.Title);
                if (BlockedSongList.All(a => ((a.Artist != blockSong.Artist) ||
                                              (a.Title != blockSong.Title))
                    ))
                {
                    BlockedSongList.Add(new ArtistTitleToBind(blockSong.Artist, blockSong.Title));
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
            FilterAndBindData();
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

        private void BtnPlayerOpen_OnClick(object sender, RoutedEventArgs e)
        {
            OpenAndPlaySelected();
        }

        private void OpenAndPlaySelected()
        {
            OpenAndPlayByIndex(DataGridAudio.SelectedIndex);
            
        }

        private void OpenAndPlayByIndex(int songIndex)
        {
            if (songIndex >= DataGridAudio.Items.Count || songIndex <= -1)
                songIndex = 0;
            var song = (Audio) DataGridAudio.Items[songIndex];
            Uri uri = song.Url;
            _mediaPlayer.Open(uri);
            MediaPlayerPlay();
            TbPlayerSong.Text = song.Artist + " - " + song.Title;
            TbPlayerSong.ToolTip = TbPlayerSong.Text;
            ProgressBarPlayer.Maximum = song.Duration;
            var time = new TimeSpan(0, 0, (int)song.Duration);
            String emptyZero = "";
            if (time.Seconds < 10)
                emptyZero = "0";
            TbPlayerTime.Text = "-" + time.Minutes + ":" + emptyZero + time.Seconds;
        }

        private void MediaPlayerPlay()
        {
            _mediaPlayer.Play();
            PlayerState = MediaState.Play;
            timer.Start();
        }

        private void MediaPlayerPause()
        {
            _mediaPlayer.Pause();
            PlayerState = MediaState.Pause;
            timer.Stop();
        }

        private void MediaPlayerEnded(object sender, EventArgs eventArgs)
        {
            if (_playerRepeatSong)
                _mediaPlayer.Position = new TimeSpan(0);
            else if (!_playerShuffle)
            {
                PlayNextSong();
            }
            else
            {
                PlayRandomSong();
            }
        }

        private void BtnPlayerPlayPause_OnClick(object sender, RoutedEventArgs e)
        {
            switch (PlayerState)
            {
                case MediaState.Play:
                    MediaPlayerPause();
                    break;
                case MediaState.Pause:
                    MediaPlayerPlay();
                    break;
                default:
                    OpenAndPlaySelected();
                    break;
            }
        }

        private void BtnPlayerPrev_OnClick(object sender, RoutedEventArgs e)
        {
            int curIndex = _fileteredRecomendedList.FindIndex(
                audio => audio.Url == _mediaPlayer.Source);
            OpenAndPlayByIndex(--curIndex);
        }

        private void BtnPlayerNext_OnClick(object sender, RoutedEventArgs e)
        {
            PlayNextSong();
        }

        private void PlayNextSong()
        {
            int curIndex = _fileteredRecomendedList.FindIndex(
                audio => audio.Url == _mediaPlayer.Source);
            OpenAndPlayByIndex(++curIndex);
        }

        private void PlayRandomSong()
        {
            int rndIndex = rnd.Next(_fileteredRecomendedList.Count);
            OpenAndPlayByIndex(rndIndex);
        }
        private void SldVolume_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _mediaPlayer.Volume = SldVolume.Value;
        }

        private void ProgressBarPlayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var x = e.GetPosition(ProgressBarPlayer).X;
            var ratio = x/ProgressBarPlayer.ActualWidth;
            _mediaPlayer.Position = new TimeSpan(0, 0, (int)(ratio*ProgressBarPlayer.Maximum));
            UpdateProgressBarNTime();
        }

        private void MenuItemCopeName_OnClick(object sender, RoutedEventArgs e)
        {
            var audio = (Audio) DataGridAudio.SelectedItem;
            Clipboard.SetText(audio.GetArtistDashTitle());
        }

        private void BtnPlayerRepeat_OnClick(object sender, RoutedEventArgs e)
        {
            _playerRepeatSong = !_playerRepeatSong;
            Brush brushFill = _playerRepeatSong ? Brushes.Black : null;
            Brush brushStroke = _playerRepeatSong
                ? Brushes.WhiteSmoke
                : (SolidColorBrush)(new BrushConverter().ConvertFrom("#333333"));
            foreach (Polygon polygon in PanelBtnRepeat.Children)
            {
                polygon.Fill = brushFill;
                polygon.Stroke = brushStroke;
            }
        }

        private void BtnPlayerShuffle_OnClick(object sender, RoutedEventArgs e)
        {
            _playerShuffle = !_playerShuffle;
            Brush brushFill = _playerShuffle ? Brushes.Black : null;
            Brush brushStroke = _playerShuffle
                ? Brushes.WhiteSmoke
                : (SolidColorBrush) (new BrushConverter().ConvertFrom("#333333"));
            foreach (Polygon polygon in GridButtonShuffle.Children)
            {
                polygon.Fill = brushFill;
                polygon.Stroke = brushStroke;
            }
        }
    }
}
