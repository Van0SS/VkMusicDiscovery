using System;
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

namespace VkMusicDiscovery
{
    /// <summary>
    /// Interaction logic for WindowBlockList.xaml
    /// </summary>
    public partial class WindowBlockList : Window
    {
        private MainWindow _mainWindow;

        public WindowBlockList()
        {
            InitializeComponent();
        }
        private void Window_Activated(object sender, EventArgs e)
        {
            _mainWindow = Owner as MainWindow;
            DataGridArtists.ItemsSource = _mainWindow.BlockedArtistList;
            DataGridSongs.ItemsSource = _mainWindow.BlockedSongList;
        }


        private void BtnClear_OnClick(object sender, RoutedEventArgs e)
        {
            ClearCurList();
        }

        private void ClearCurList()
        {
            if (TabControlLists.SelectedIndex == 0)
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
            ParseFiles();
        }

        /// <summary>
        /// Замещение текущего листа.
        /// </summary>
        private void BtnImport_OnClick(object sender, RoutedEventArgs e)
        {
            ClearCurList();
            ParseFiles();
        }

        /// <summary>
        /// Считывание файлов и запись в текущий лист. utf-8
        /// </summary>
        private void ParseFiles()
        {
            var loadDialog = new OpenFileDialog();
            loadDialog.Multiselect = true;
            bool isArtists = (TabControlLists.SelectedIndex == 0);
            var txtAllFilter = "|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            var avkFilter = "Artists files (*.avk)|*.avk" + txtAllFilter;
            var svkFilter = "Songs files (*.svk)|*.svk" + txtAllFilter;

            loadDialog.Filter = isArtists ? avkFilter : svkFilter;
            if (loadDialog.ShowDialog() != true) return;
            var wrongsFiles = "";
            foreach (var fileName in loadDialog.FileNames)
            {
                
                try
                {
                    using (var fileReader = new StreamReader(fileName))
                    {
                        string line;
                        while ((line = fileReader.ReadLine()) != null)
                        {
                            if (isArtists)
                            {
                                _mainWindow.BlockedArtistList.Add(new ArtistToBind(line));
                            }
                            else
                            {
                                var indexOfSep = line.IndexOf(" - ");
                                var artist = line.Substring(0, indexOfSep);
                                var title = line.Substring(indexOfSep + 3);
                                _mainWindow.BlockedSongList.Add(new ArtistTitleToBind(artist, title));
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    wrongsFiles += "\n" + fileName;
                }
            }
            if (wrongsFiles != "")
            {
                MessageBox.Show("Next files can't open:" + wrongsFiles);
            }
            if (isArtists)
                DataGridArtists.Items.Refresh();
            else
                DataGridSongs.Items.Refresh();
        }

        /// <summary>
        /// Экспорт в файл. utf-8
        /// </summary>
        private void BtnExport_OnClick(object sender, RoutedEventArgs e)
        {
            var saveDialog = new CommonSaveFileDialog();
            if (TabControlLists.SelectedIndex == 0)
            {
                saveDialog.DefaultExtension = "avk";
                saveDialog.DefaultFileName = "Artists";
                if (saveDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    var sortedArtists = _mainWindow.BlockedArtistList.OrderBy(x => x.Artist).ToList();
                    using (StreamWriter fileWriter = new StreamWriter(saveDialog.FileName))
                    {
                        foreach (var artist in sortedArtists)
                        {
                            fileWriter.WriteLine(artist.Artist);
                        }
                    }
                }
            }
            else
            {
                saveDialog.DefaultExtension = "svk";
                saveDialog.DefaultFileName = "Songs";
                if (saveDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    var sortedSongs = _mainWindow.BlockedSongList.OrderBy(x => x.Artist).ToList();
                    using (StreamWriter fileWriter = new StreamWriter(saveDialog.FileName))
                    {
                        foreach (var artist in sortedSongs)
                        {
                            fileWriter.WriteLine(artist.Artist + " - " + artist.Title);
                        }
                    }
                }
            }


        }
    }
}
