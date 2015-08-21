using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace VkMusicDiscovery
{
    public partial class MainWindow
    {
        #region - Fields -

        private MediaPlayer _mediaPlayer = new MediaPlayer();
        /// <summary>
        /// Текущее состояние плеера(сам он не поддерживает).
        /// </summary>
        private MediaState _playerState;
        /// <summary>
        /// Будет ли плеер играть одну песню постоянно.
        /// </summary>
        private bool _playerRepeatSong;
        /// <summary>
        /// Будет ли плеер играть случайную песню.
        /// </summary>
        private bool _playerShuffle;
        /// <summary>
        /// Таймер обновляющий прогресс и оставшееся время.
        /// </summary>
        private DispatcherTimer _timer;

        readonly Random _rnd = new Random();

        private MediaState PlayerState
        {
            get { return _playerState; }
            set
            {
                _playerState = value;
                ChangePlayButtonState();
            }
        }

        #endregion - Fields -
        //---------------------------------------------------------------------------------------------
        #region - Private methods -
        private void PlayerInitialization()
        {
            _mediaPlayer.MediaEnded += MediaPlayerEnded;
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1); //Обновлять каждую секунду.
            _timer.Tick += timer_Tick;
        }

        private void UpdatePlayerProgressNTime()
        {
            ProgressBarPlayer.Value = _mediaPlayer.Position.TotalSeconds;
            if (!_mediaPlayer.NaturalDuration.HasTimeSpan)
                return;
            var elapsedTime = _mediaPlayer.NaturalDuration.TimeSpan - _mediaPlayer.Position;
            String emptyZero = "";
            if (elapsedTime.Seconds < 10) //Чтобы не было: 3:4 а было: 3:04
                emptyZero = "0";
            TbPlayerTime.Text = "-" + elapsedTime.Minutes + ":" + emptyZero + elapsedTime.Seconds;
        }

        private void OpenAndPlayByIndex(int songIndex)
        {
            if (songIndex >= DataGridAudio.Items.Count || songIndex <= -1)
                songIndex = 0;
            var song = (Audio)DataGridAudio.Items[songIndex];
            _mediaPlayer.Open(song.Url);
            MediaPlayerPlay();
            TbPlayerSong.Text = song.GetArtistDashTitle();
            TbPlayerSong.ToolTip = TbPlayerSong.Text;
            //Песня ещё не загрузилась, поэтому длительность лучше взять из объекта Audio т.е. то что сервер говорит.
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
            _timer.Start();
        }

        private void MediaPlayerPause()
        {
            _mediaPlayer.Pause();
            PlayerState = MediaState.Pause;
            _timer.Stop();
        }

        private void ChangePlayButtonState()
        {
            BtnPlayerPlayPause.Content =
                FindResource(_playerState == MediaState.Play ? "PausePic" : "PlayPic");
        }
        /// <summary>
        /// Найти в списке текущую песню(по url) и програть следующую.
        /// </summary>
        private void PlayNextSong()
        {
            int curIndex = _fileteredRecomendedList.FindIndex(
                audio => audio.Url == _mediaPlayer.Source);
            OpenAndPlayByIndex(++curIndex);
        }

        private void PlayRandomSong()
        {
            int rndIndex = _rnd.Next(_fileteredRecomendedList.Count);
            OpenAndPlayByIndex(rndIndex);
        }

        #endregion - Private methods -
        //---------------------------------------------------------------------------------------------
        #region - Event Handlers -

        private void timer_Tick(object sender, EventArgs e)
        {
            UpdatePlayerProgressNTime();
        }

        private void BtnPlayerOpen_OnClick(object sender, RoutedEventArgs e)
        {
            //Проиграть первый из выделенных файлов.
            OpenAndPlayByIndex(DataGridAudio.SelectedIndex);
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
                default: //При остальных состояниях повторять действия кнопки Open.
                    OpenAndPlayByIndex(DataGridAudio.SelectedIndex);
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

        private void SldVolume_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _mediaPlayer.Volume = SldVolume.Value;
        }

        /// <summary>
        /// Клик на полосе перемотки.
        /// </summary>
        private void ProgressBarPlayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var x = e.GetPosition(ProgressBarPlayer).X;
            var ratio = x / ProgressBarPlayer.ActualWidth;
            _mediaPlayer.Position = new TimeSpan(0, 0, (int)(ratio * ProgressBarPlayer.Maximum));
            UpdatePlayerProgressNTime();
        }

        private void BtnPlayerRepeat_OnClick(object sender, RoutedEventArgs e)
        {
            _playerRepeatSong = !_playerRepeatSong; //Сменить состояние кнопки.
            Brush brushFill = _playerRepeatSong ? Brushes.Black : null; //Если включено то залить чёрным.
            Brush brushStroke = _playerRepeatSong
                ? Brushes.WhiteSmoke //И светлая рамка.
                : (SolidColorBrush)(new BrushConverter().ConvertFrom("#333333"));//Иначё тёмная.
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
                : (SolidColorBrush)(new BrushConverter().ConvertFrom("#333333"));
            foreach (Polygon polygon in GridButtonShuffle.Children)
            {
                polygon.Fill = brushFill;
                polygon.Stroke = brushStroke;
            }
        }

        //Когда текущая песня проиграла.
        private void MediaPlayerEnded(object sender, EventArgs eventArgs)
        {
            if (_playerRepeatSong) //Если стоит повтор то перемотать на начало песни.
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
        #endregion - Event Handlers -
    }
}
