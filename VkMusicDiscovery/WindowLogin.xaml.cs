using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using VkMusicDiscovery.Enums;

namespace VkMusicDiscovery
{
    /// <summary>
    /// Interaction logic for WindowLogin.xaml
    /// </summary>
    public partial class WindowLogin : Window
    {
        public string AccessToken { get; private set; }

        public int UserId { get; private set; }

        private bool _modeLogIn = true;

        //Id приложения в ВК.
        private const int AppId = 4533969;
        //Права доступа только к аудио.
        private const int Scope = (int)(ScopeType.Audio);
        public WindowLogin()
        {
            InitializeComponent();
            //SetSilent(WebBrowserLogin, true);
        }

        public void Autorize() //Авторизация клиентских приложений.
        {
            WebBrowserLogin.Navigate(String.Format("http://api.vk.com/oauth/authorize?client_id={0}&scope={1}&display=popup&response_type=token", AppId, Scope));
        }

        public void LogOut()
        {
            _modeLogIn = false;
            TbInstruction.Text = "Log out manually:";
            WebBrowserLogin.Navigate("https://login.vk.com/?act=logout&hash=14466908cac58bbe4b&_origin=http://vk.com");
        }

        void WebBrowserLogin_Navigated(object sender, NavigationEventArgs e)
        {
            SetSilent(WebBrowserLogin, true); // make it silent
        }

        private void WebBrowserLogin_OnLoadCompleted(object sender, NavigationEventArgs e)
        {
            //Если url содержит токен, то забираем и выходим.
            if ((_modeLogIn) &&(e.Uri.ToString().IndexOf("access_token") != -1))
            {
                int expiresIn = 0;
                Regex myReg = new Regex(@"(?<name>[\w\d\x5f]+)=(?<value>[^\x26\s]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (Match m in myReg.Matches(e.Uri.ToString()))
                {
                    if (m.Groups["name"].Value == "access_token")
                    {
                        AccessToken = m.Groups["value"].Value;
                    }
                    else if (m.Groups["name"].Value == "user_id")
                    {
                        UserId = Convert.ToInt32(m.Groups["value"].Value);
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

        public static void SetSilent(WebBrowser browser, bool silent)
        {
            if (browser == null)
                throw new ArgumentNullException("browser");

            // get an IWebBrowser2 from the document
            IOleServiceProvider sp = browser.Document as IOleServiceProvider;
            if (sp != null)
            {
                Guid IID_IWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
                Guid IID_IWebBrowser2 = new Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E");

                object webBrowser;
                sp.QueryService(ref IID_IWebBrowserApp, ref IID_IWebBrowser2, out webBrowser);
                if (webBrowser != null)
                {
                    webBrowser.GetType().InvokeMember("Silent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.PutDispProperty, null, webBrowser, new object[] { silent });
                }
            }
        }


        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IOleServiceProvider
        {
            [PreserveSig]
            int QueryService([In] ref Guid guidService, [In] ref Guid riid, [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            AccessToken = TbxToken.Text;
            Close();
        }
    }
}
