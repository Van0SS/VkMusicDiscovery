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
        public int LoginUserId;

        private const string VkApiVersion = "v=5.28";

        public VkApi(string accessToken, int userId)
        {
            AccessToken = accessToken;
            LoginUserId = userId;
        }


        public List<Audio> AudioGetRecommendations(int count = 100, bool shuffle = false, int offset = 0, int? userId = null,  string targetAudio = "")
        {
            var parameters = new NameValueCollection();
            var audioList = new List<Audio>();

            parameters["user_id"] = LoginUserId.ToString();
            parameters["count"] = count.ToString();
            parameters["shuffle"] =  Convert.ToInt32(shuffle).ToString();
            parameters["offset"] = offset.ToString();
            XmlDocument recomendAudiosXml = ExecuteCommand("audio.getRecommendations", parameters);
            recomendAudiosXml.Save("aaaa.xml");
            XmlNode reNode = recomendAudiosXml.SelectSingleNode("response");
            XmlNodeList reNode2 = reNode.ChildNodes;

            foreach (XmlNode audioNode in
                reNode2.Item(1).SelectNodes("audio"))
            {
                var curAudio = new Audio();

                curAudio.Id = Convert.ToUInt32(audioNode.SelectSingleNode("id").InnerText);
                curAudio.OwnerId = Convert.ToInt32(audioNode.SelectSingleNode("owner_id").InnerText);
                curAudio.Artist = audioNode.SelectSingleNode("artist").InnerText;
                curAudio.Title = audioNode.SelectSingleNode("title").InnerText;
                curAudio.Duration = Convert.ToUInt32(audioNode.SelectSingleNode("duration").InnerText);
                curAudio.Url = new Uri(audioNode.SelectSingleNode("url").InnerText);

                var lyricsIdNode = audioNode.SelectSingleNode("lyrics_id");
                if (lyricsIdNode != null)
                    curAudio.LyricsId = Convert.ToUInt32(lyricsIdNode.InnerText);
                var genreIdNode = audioNode.SelectSingleNode("genre_id");
                if (genreIdNode != null)
                    curAudio.GenreId = (AudioGenres) Convert.ToUInt32(genreIdNode.InnerText);

                audioList.Add(curAudio);
            }
            return audioList;
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
            string request = String.Format("https://api.vk.com/method/{0}.xml?{1}&{2}&access_token={3}", name,
                String.Join("&", from item in qs.AllKeys select item + "=" + qs[item]), VkApiVersion, AccessToken);
            result.Load(request);

            return result;
        }

    }
}
