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
        /// Добавление в текущий лист.
        /// </summary>
        private void BtnAdd_OnClick(object sender, RoutedEventArgs e)
        {
            var files = ShowOpenFileDialog(false, _curTabType);
            if (files == null) return;
            ParseFiles(files, _curTabType);
            RefreshList(_curTabType);
        }

        /// <summary>
        /// Замещение текущего листа.
        /// </summary>
        private void BtnOpen_OnClick(object sender, RoutedEventArgs e)
        {
            var file = ShowOpenFileDialog(false, _curTabType);
            if (file == null) return;
            OpenFile(file[0], _curTabType);
            RefreshList(_curTabType);
        }

        ///
        private string[] ShowOpenFileDialog(bool multiselect, BlockTabType tabType)
        {
            var loadDialog = new OpenFileDialog();
            loadDialog.Multiselect = multiselect;
            var txtAllFilter = "|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            var avkFilter = "Artists files (*.avk)|*.avk" + txtAllFilter;
            var svkFilter = "Songs files (*.svk)|*.svk" + txtAllFilter;

            loadDialog.Filter = _curTabType == BlockTabType.Artists ? avkFilter : svkFilter;
            if (loadDialog.ShowDialog() != true) return null;
            return loadDialog.FileNames;

        }

        private void OpenFile(string fileName, BlockTabType tabType)
        {
            try
            {
                ClearCurList();
                _mainWindow.ParseFile(fileName, tabType);
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

        private void RefreshList(BlockTabType tabType)
        {
            if (tabType == BlockTabType.Artists)
                DataGridArtists.Items.Refresh();
            else
                DataGridSongs.Items.Refresh();
        }

        private void ParseFiles(string[] fileNames, BlockTabType tabType)
        {
            var wrongsFiles = "";
            foreach (var fileName in fileNames)
            {

#if !DEBUG
                try
                {
#endif
                _mainWindow.ParseFile(fileName, tabType);
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
        /// Экспорт в файл. utf-8
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

        private void TabControlLists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _curTabType = TabControlLists.SelectedIndex == 0 ? BlockTabType.Artists : BlockTabType.Songs;
            SetTitle();
        }
    }
}
