using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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

        private List<MainWindow.ArtistToBind> _blockedArtistList;
        private List<MainWindow.ArtistTitleToBind> _blockedSongList;
        public WindowBlockList(List<MainWindow.ArtistToBind> blockedArtistList, List<MainWindow.ArtistTitleToBind> blockedSongList)
        {
            InitializeComponent();
            DataGridArtists.ItemsSource = blockedArtistList;
            DataGridSongs.ItemsSource = blockedSongList;
            _blockedArtistList = blockedArtistList;
            _blockedSongList = blockedSongList;
        }

        private void BtnImport_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnClear_OnClick(object sender, RoutedEventArgs e)
        {
            if (TabControlLists.SelectedIndex == 0)
            {
                _blockedArtistList.Clear();
                DataGridArtists.Items.Refresh();
            }
            else
            {
                _blockedSongList.Clear();
                DataGridSongs.Items.Refresh();
            }
            
        }
    }
}
