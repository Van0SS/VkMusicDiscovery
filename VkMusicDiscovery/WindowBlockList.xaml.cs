using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using VkMusicDiscovery.Enums;

namespace VkMusicDiscovery
{
    /// <summary>
    /// Interaction logic for WindowBlockList.xaml
    /// </summary>
    public partial class WindowBlockList : Window
    {
        private MainWindow _mainWindow;
        private const string TitleString = "Block list";

        private BlockTabType _curTabType = BlockTabType.Artists;

        public WindowBlockList()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Установить в заголовок окна название текущего файла блокировки.
        /// </summary>
        private void SetTitle()
        {
            Title = (_curTabType == BlockTabType.Artists ? Settings.PathCurUsedArtists : Settings.PathCurUsedSongs)
                + " - " + TitleString;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            _mainWindow = Owner as MainWindow;
            DataGridArtists.ItemsSource = _mainWindow.BlockedArtistList;
            DataGridSongs.ItemsSource = _mainWindow.BlockedSongList;

            SetTitle();
        }

        private void BtnClear_OnClick(object sender, RoutedEventArgs e)
        {
            ClearCurList();
        }

        /// <summary>
        /// Очистить текущий лист
        /// </summary>
        private void ClearCurList()
        {
            if (_curTabType == BlockTabType.Artists)
            {
                _mainWindow.BlockedArtistList.Clear();
                DataGridArtists.Items.Refresh();
            }
            else
            {
                _mainWindow.BlockedSongList.Clear();
                DataGridSongs.Items.Refresh();
            }
        }

        /// <summary>
        /// Добавление выбранной коллекции в текущий лист.
        /// </summary>
        private void BtnAddCollection_OnClick(object sender, RoutedEventArgs e)
        {
            var files = ShowOpenFileDialog(false, _curTabType);
            if (files == null) return;
            ParseFiles(files, _curTabType);
            RefreshList(_curTabType);
        }

        /// <summary>
        /// Добавление имён песне в текущий лист.
        /// </summary>
        private void BtnAddHeadersOnClick(object sender, RoutedEventArgs e)
        {
            var files = ShowOpenFileDialog(true, _curTabType, true);
            if (files == null) return;
            ParsreFilesHeaders(files, _curTabType);
            RefreshList(_curTabType);
        }

        /// <summary>
        /// Выбрать новую коллекцию блокировки.
        /// </summary>
        private void BtnOpen_OnClick(object sender, RoutedEventArgs e)
        {
            var file = ShowOpenFileDialog(false, _curTabType);
            if (file == null) return;
            OpenFile(file[0], _curTabType);
            RefreshList(_curTabType);
        }

        /// <summary>
        /// Открыть диалог открытия файлов.
        /// </summary>
        /// <param name="multiselect">Можно ли выбрать много файлов</param>
        /// <param name="tabType">Тип блокировки</param>
        /// <param name="headers">Использовать файлы песен или листов</param>
        /// <returns></returns>
        private string[] ShowOpenFileDialog(bool multiselect, BlockTabType tabType, bool headers = false)
        {
            var loadDialog = new OpenFileDialog();
            loadDialog.Multiselect = multiselect;
            var txtFilter = "|Text files (*.txt)|*.txt";
            var allFilter = "|All files (*.*)|*.*";
            var avkFilter = "Artists files (*.avk)|*.avk" + txtFilter + allFilter;
            var svkFilter = "Songs files (*.svk)|*.svk" + txtFilter + allFilter;

            var musicFilter = "Audio|*.mp3;*.wma;*.flac;*.m4a;*.aac;*.wav";

            if (headers != true)
                loadDialog.Filter = _curTabType == BlockTabType.Artists ? avkFilter : svkFilter;
            else
            {
                loadDialog.Filter = (musicFilter + allFilter);
            }
            if (loadDialog.ShowDialog() != true) return null;
            return loadDialog.FileNames;

        }

        /// <summary>
        /// Замещение текущего листа.
        /// </summary>
        private void OpenFile(string fileName, BlockTabType tabType)
        {
            try
            {
                ClearCurList();
                _mainWindow.BlockCollection(fileName, tabType);
                if (tabType == BlockTabType.Artists)
                    Settings.PathCurUsedArtists = fileName;
                else
                    Settings.PathCurUsedSongs = fileName;
                SetTitle();
            }
            catch (Exception fileException)
            {
                MessageBox.Show("Can't open file: " + fileException);
                throw;
            }
        }

        /// <summary>
        /// Вручную обновить лист блокировки.
        /// </summary>
        private void RefreshList(BlockTabType tabType)
        {
            if (tabType == BlockTabType.Artists)
                DataGridArtists.Items.Refresh();
            else
                DataGridSongs.Items.Refresh();
        }

        /// <summary>
        /// Добавить в список блокировки коллекции.
        /// </summary>
        /// <param name="fileNames">Коллекция</param>
        private void ParseFiles(string[] fileNames, BlockTabType tabType)
        {
            var wrongsFiles = "";
            foreach (var fileName in fileNames)
            {

#if !DEBUG
                try
                {
#endif
                _mainWindow.BlockCollection(fileName, tabType);
#if !DEBUG
                }
                catch (Exception file)
                {
                    wrongsFiles += "\n" + file.Message;
                }
#endif
            }
            if (wrongsFiles != "")
            {
                MessageBox.Show("Next files can't open:" + wrongsFiles);
            }
        }

        /// <summary>
        /// Добавить в список блокировки названия песен.
        /// </summary>
        /// <param name="fileHeaders"></param>
        /// <param name="tabType"></param>
        private void ParsreFilesHeaders(string[] fileHeaders, BlockTabType tabType)
        {
            var wrongsFiles = "";
            foreach (var fileHeader in fileHeaders)
            {
#if !DEBUG
                try
                {
#endif
                    _mainWindow.BlockHeader(fileHeader, tabType);
#if !DEBUG
                }
                catch (Exception file)
                {
                    wrongsFiles += "\n" + file.Message;
                }
#endif
            }
            if (wrongsFiles != "")
            {
                MessageBox.Show("Next files headers can't parse:" + wrongsFiles);
            }
        }

        /// <summary>
        /// Сохранить текущий список в файл.
        /// </summary>
        private void BtnSave_OnClick(object sender, RoutedEventArgs e)
        {
            if (_curTabType == BlockTabType.Artists)
            {
                if (Settings.PathCurUsedArtists == "New")
                    SaveFile();
                _mainWindow.WriteFile(Settings.PathCurUsedArtists, BlockTabType.Artists);   
            }
            else
            {
                if (Settings.PathCurUsedSongs == "New")
                    SaveFile();
                _mainWindow.WriteFile(Settings.PathCurUsedSongs, BlockTabType.Songs);                
            }
        }

        /// <summary>
        /// Экспорт в новый файл. utf-8
        /// </summary>
        private void SaveFile()
        {
            var saveDialog = new SaveFileDialog();
            if (_curTabType == BlockTabType.Artists)
            {
                saveDialog.AddExtension = true;
                saveDialog.DefaultExt = ".avk";
                saveDialog.FileName = "Artists.avk";

                if (saveDialog.ShowDialog() == true)
                {
                    _mainWindow.WriteFile(saveDialog.FileName, BlockTabType.Artists);
                    Settings.PathCurUsedArtists = saveDialog.FileName;
                }
            }
            else
            {
                saveDialog.FileName = "Songs.svk";
                if (saveDialog.ShowDialog() == true)
                {
                    _mainWindow.WriteFile(saveDialog.FileName, BlockTabType.Songs);
                    Settings.PathCurUsedSongs = saveDialog.FileName;
                }
            }
            SetTitle();
        }

        private void BtnNew_OnClick(object sender, RoutedEventArgs e)
        {
            NewFile(_curTabType);
        }

        private void BtnSaveAs_OnClick(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        /// <summary>
        /// Новый файл списка блокировки.
        /// </summary>
        /// <param name="tabType"></param>
        private void NewFile(BlockTabType tabType)
        {
            ClearCurList();
            if (tabType == BlockTabType.Artists)
            {
                Settings.PathCurUsedArtists = "New";
            }
            else
            {
                Settings.PathCurUsedSongs = "New";
            }
            SetTitle();
        }

        /// <summary>
        /// Сменить вкладку
        /// </summary>
        private void TabControlLists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _curTabType = TabControlLists.SelectedIndex == 0 ? BlockTabType.Artists : BlockTabType.Songs;
            SetTitle();
        }
    }
}
