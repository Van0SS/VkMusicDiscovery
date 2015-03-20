using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Interaction logic for WindowBlockList.xaml
    /// </summary>
    public partial class WindowBlockList : Window
    {
        public WindowBlockList(List<MainWindow.ArtistToBind> blockedArtistList, List<MainWindow.ArtistTitleToBind> blockedSongList)
        {
            InitializeComponent();
            DataGridArtists.ItemsSource = blockedArtistList;
            DataGridSongs.ItemsSource = blockedSongList;
        }

        private void BtnImport_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
