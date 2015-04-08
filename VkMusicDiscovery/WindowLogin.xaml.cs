using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Navigation;
using VkMusicDiscovery.Enums;

namespace VkMusicDiscovery
{
    /// <summary>
    /// Interaction logic for WindowLogin.xaml
    /// </summary>
    public partial class WindowLogin : Window
    {
        private string _accessToken;

        public string AccessToken
        {
            get { return _accessToken; }
        }

        private int _userId;
        public int UserId
        {
            get { return _userId; }
        }

        //Id приложения в ВК.
        private const int AppId = 4533969;
        //Права доступа только к аудио.
        private const int Scope = (int)(ScopeType.Audio);
        public WindowLogin()
        {
            InitializeComponent();
            //Пытаемся авторизоваться.
            Autorize();
        }

        public void Autorize() //Авторизация клиентских приложений.
        {
            WebBrowserLogin.Navigate(String.Format("http://api.vk.com/oauth/authorize?client_id={0}&scope={1}&display=popup&response_type=token", AppId, Scope));
        }
        private void WebBrowserLogin_OnLoadCompleted(object sender, NavigationEventArgs e)
        {
            //Если url содержит токен, то забираем и выходим.
            if (e.Uri.ToString().IndexOf("access_token") != -1)
            {
                int expiresIn = 0;
                Regex myReg = new Regex(@"(?<name>[\w\d\x5f]+)=(?<value>[^\x26\s]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (Match m in myReg.Matches(e.Uri.ToString()))
                {
                    if (m.Groups["name"].Value == "access_token")
                    {
                        _accessToken = m.Groups["value"].Value;
                    }
                    else if (m.Groups["name"].Value == "user_id")
                    {
                        _userId = Convert.ToInt32(m.Groups["value"].Value);
                    }
                    else if (m.Groups["name"].Value == "expires_in")
                    {
                        expiresIn = Convert.ToInt32(m.Groups["value"].Value);
                    }
                    // еще можно запомнить срок жизни access_token - expires_in,
                }
                Close(); //Выход в главные окно программы.
            }
        }
    }
}
