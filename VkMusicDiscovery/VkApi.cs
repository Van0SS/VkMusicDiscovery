using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Xml;
using VkMusicDiscovery.Enums;

namespace VkMusicDiscovery
{
    class VkApi
    {
        public string AccessToken = "";
        public int UserId;
                    WebBrowser webBrowserLogin = new WebBrowser();

        public VkApi(string accessToken, int userId)
        {
            AccessToken = accessToken;
            UserId = userId;
        }





        public XmlDocument AudioGetRecommendations()
        {
            NameValueCollection parameters = new NameValueCollection();
            //parameters["count"] = count.ToString();
            parameters["user_id"] = UserId.ToString();
            return ExecuteCommand("audio.getRecommendations", parameters);
        }

        /// <summary>
        /// Выполнить команду на сервере ВК
        /// </summary>
        /// <param name="name">Название функции API</param>
        /// <param name="qs">Параметры</param>
        /// <returns></returns>
        private XmlDocument ExecuteCommand(string name, NameValueCollection qs)
        {
            XmlDocument result = new XmlDocument();
            string request = String.Format("https://api.vk.com/method/{0}.xml?{1}&access_token={2}", name,
                String.Join("&", from item in qs.AllKeys select item + "=" + qs[item]), AccessToken);
            result.Load(request);
            return result;
        }

    }
}
