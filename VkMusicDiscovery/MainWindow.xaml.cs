using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace VkMusicDiscovery
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VkApi _vkApi;
        private List<Audio> _audiosRecomendedList;
        private IEnumerable<Audio> _fileteredRecomendedList;
        public MainWindow()
        {
            InitializeComponent();
            WindowLogin windowLogin = new WindowLogin();
            windowLogin.ShowDialog();
            _vkApi = new VkApi(windowLogin.AccessToken, windowLogin.UserId);
            _audiosRecomendedList = _vkApi.AudioGetRecommendations(10, true);


           // AudioDataGrid.ItemsSource
            _fileteredRecomendedList = _audiosRecomendedList;
            AudioDataGrid.ItemsSource = _fileteredRecomendedList;




            //musicDocument.Save("aaaa.xml");
            //System.Diagnostics.Process.Start(Directory.GetCurrentDirectory());
        }

        private void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            int count = Convert.ToInt32(TbCount.Text);
            bool random = CbxRandom.IsChecked.Value;
            _audiosRecomendedList =_vkApi.AudioGetRecommendations(count, random);
            FilterByLangAndBindData();
        }

        private void BtnDownloadall_OnClick(object sender, RoutedEventArgs e)
        {
            var dirDialog = new CommonOpenFileDialog {IsFolderPicker = true};
            if (dirDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {

                foreach (var track in _fileteredRecomendedList)
                {
                    string fileName = track.Artist + " - " + track.Title + ".mp3"; //В вк пока только мр3.
                    
                    if (fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) != -1) //Если есть недопустимые символы, то удалить.
                        fileName = string.Concat(fileName.Split(System.IO.Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                    
                    if (fileName.Length > 250) //Если имя файла длинее 250 символов - обрезать.
                        fileName = fileName.Substring(0, 250);
                    
                    new WebClient().DownloadFileAsync(track.Url, dirDialog.FileName +
                        '\\' + fileName);
                }
            }
        }

        private void RbtnsLang_OnChecked(object sender, RoutedEventArgs e)
        {
            FilterByLangAndBindData();
        }

        private void FilterByLangAndBindData()
        {
            if (RbtnLangRu.IsChecked == true)
            {
                _fileteredRecomendedList = from track in _audiosRecomendedList
                    where Regex.IsMatch((track.Artist + track.Title), "[А-Яа-я]")
                    select track;
            }
            else if (RbtnLangEng.IsChecked == true)
            {
                _fileteredRecomendedList = from track in _audiosRecomendedList
                    where !Regex.IsMatch((track.Artist + track.Title), "[А-Яа-я]")
                    select track;
            }
            else
            {
                _fileteredRecomendedList = _audiosRecomendedList;
                return;
            }
            AudioDataGrid.ItemsSource = _fileteredRecomendedList;
        }
    }
}
