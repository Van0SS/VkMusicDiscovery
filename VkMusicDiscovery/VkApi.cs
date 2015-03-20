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

            //Отправляем только отличные от стандартных параметры.
            if ((userId != LoginUserId) && (userId != null)) //Юзер по дефолту залогиненый.
                parameters["user_id"] = userId.ToString();
            if (count != 100) //По дефолту 100 песен.
                parameters["count"] = count.ToString();
            if (shuffle == true) //По дефолту нет шафла.
                parameters["shuffle"] = 1.ToString();
            if (offset != 0)
                parameters["offset"] = offset.ToString();
            XmlDocument recomendAudiosXml = ExecuteCommand("audio.getRecommendations", parameters);
            
            //Извечение данных из структуры вида:
            //<response>
            //  <count>400</count>
            //  <items list="true">
            //      <audio>
            //      </audio>
            //      <audio>
            XmlNode reNode = recomendAudiosXml.SelectSingleNode("response");
            if (reNode == null)
            {
                var errNode = recomendAudiosXml.SelectSingleNode("error");
                if (errNode != null)
                {
                    var errorMessage = "Error code: " + errNode.SelectSingleNode("error_code").InnerText
                                       + "\nError message: " + errNode.SelectSingleNode("error_msg").InnerText;
                    throw new Exception(errorMessage);
                }
                
            }
            XmlNodeList reNode2 = reNode.ChildNodes;

            //  <items list="true"> // По другому не выбирается.
            foreach (XmlNode audioNode in reNode2.Item(1).SelectNodes("audio"))
            {
                var curAudio = new Audio();

                curAudio.Id = Convert.ToUInt32(audioNode.SelectSingleNode("id").InnerText);
                curAudio.OwnerId = Convert.ToInt32(audioNode.SelectSingleNode("owner_id").InnerText);
                curAudio.Artist = audioNode.SelectSingleNode("artist").InnerText;
                curAudio.Title = audioNode.SelectSingleNode("title").InnerText;
                curAudio.Duration = Convert.ToUInt32(audioNode.SelectSingleNode("duration").InnerText);
                curAudio.Url = new Uri(audioNode.SelectSingleNode("url").InnerText);

                var lyricsIdNode = audioNode.SelectSingleNode("lyrics_id");
                if (lyricsIdNode != null) //Текст есть не у всех песен.
                    curAudio.LyricsId = Convert.ToUInt32(lyricsIdNode.InnerText);
                var genreIdNode = audioNode.SelectSingleNode("genre_id");
                if (genreIdNode != null) //Жанр тоже.
                    curAudio.GenreId = (AudioGenres) Convert.ToUInt32(genreIdNode.InnerText);

                curAudio.Artist = StaticFunc.ToLowerButFirstUp(curAudio.Artist);
                curAudio.Title = StaticFunc.ToLowerButFirstUp(curAudio.Title);

                //Если после конкатенации названия с добавлением " - " будет ещё такая же конструкция
                //то будут проблемы с парсингом, да и просто не красиво.
                curAudio.Artist = curAudio.Artist.Replace(" -", " ");
                curAudio.Artist = curAudio.Artist.Replace("- ", " ");
                curAudio.Title = curAudio.Title.Replace(" -", " ");
                curAudio.Title = curAudio.Title.Replace("- ", " ");

                audioList.Add(curAudio);
            }
            return audioList;
        }

        /// <summary>
        /// Выполнить команду на сервере ВК
        /// </summary>
        /// <param name="name">Название функции API</param>
        /// <param name="parameters">Параметры</param>
        /// <returns></returns>
        private XmlDocument ExecuteCommand(string name, NameValueCollection parameters)
        {
            XmlDocument result = new XmlDocument();
            string request = String.Format("https://api.vk.com/method/{0}.xml?{1}&{2}&access_token={3}", name,
                String.Join("&", from item in parameters.AllKeys select item + "=" + parameters[item]), VkApiVersion, AccessToken);
            result.Load(request);

            return result;
        }

    }
}
