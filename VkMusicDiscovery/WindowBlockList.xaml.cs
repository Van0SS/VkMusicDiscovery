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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
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
            Title = (_curTabType == BlockTabType.Artists ? Utils.PathBlockArtists : Utils.PathBlockSongs)
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
                    Utils.PathBlockArtists = fileName;
                else
                    Utils.PathBlockSongs = fileName;
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
                if (Utils.PathBlockArtists == "New")
                    SaveFile();
                _mainWindow.WriteFile(Utils.PathBlockArtists, BlockTabType.Artists);   
            }
            else
            {
                if (Utils.PathBlockSongs == "New")
                    SaveFile();
                _mainWindow.WriteFile(Utils.PathBlockSongs, BlockTabType.Songs);                
            }
        }

        private void SaveFile()
        {
            var saveDialog = new CommonSaveFileDialog();
            if (_curTabType == BlockTabType.Artists)
            {
                saveDialog.DefaultExtension = "avk";
                saveDialog.DefaultFileName = "Artists";
                if (saveDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    _mainWindow.WriteFile(saveDialog.FileName, BlockTabType.Artists);
                    Utils.PathBlockArtists = saveDialog.FileName;
                }
            }
            else
            {
                saveDialog.DefaultExtension = "svk";
                saveDialog.DefaultFileName = "Songs";
                if (saveDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    _mainWindow.WriteFile(saveDialog.FileName, BlockTabType.Songs);
                    Utils.PathBlockSongs = saveDialog.FileName;
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
                Utils.PathBlockArtists = "New";
            }
            else
            {
                Utils.PathBlockSongs = "New";
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
