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
using Microsoft.WindowsAPICodePack.Dialogs;

namespace VkMusicDiscovery
{
    public class TitleArtistValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string artOrTitle = value.ToString();
            if (artOrTitle == "")
                return new ValidationResult(false, "String can not be empty");
            return new ValidationResult(true, null);
        }
    }
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

        private void BtnImport_OnClick(object sender, RoutedEventArgs e)
        {
            var loadDialog = new CommonOpenFileDialog();
            loadDialog.Multiselect = true;
            bool isArtists = (TabControlLists.SelectedIndex == 0);
            loadDialog.DefaultExtension = isArtists ? "avk" : "svk";
            if (loadDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach (var fileName in loadDialog.FileNames)
                {
                    using (var fileReader = new StreamReader(fileName))
                    {
                        string line;
                        while ((line = fileReader.ReadLine()) != null)
                        {
                            if (isArtists)
                                _mainWindow.BlockedArtistList.Add(new ArtistToBind(line));
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
            }
            if (isArtists)
                DataGridArtists.Items.Refresh();
            else
                DataGridSongs.Items.Refresh();
        }

        private void BtnClear_OnClick(object sender, RoutedEventArgs e)
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
