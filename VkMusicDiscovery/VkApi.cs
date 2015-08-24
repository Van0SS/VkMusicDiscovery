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
        /// <summary>
        /// Токен доступа конкретного пользователя.
        /// </summary>
        public string AccessToken = "";

        public int? LoginUserId;

        private const string VkApiVersion = "v=5.28";

        public VkApi(string accessToken, int userId)
        {
            AccessToken = accessToken;
            LoginUserId = userId;
        }

        /// <summary>
        /// Поиск песен.
        /// </summary>
        /// <param name="searchString">Строка поиска</param>
        /// <param name="count">Количество</param>
        /// <param name="autoComplete">Исправлять ошибки</param>
        /// <param name="lyrics">С текстом</param>
        /// <param name="performerOnly">Только по исполнителю</param>
        /// <param name="sort">Тип сортировки</param>
        /// <param name="searchOwn">Только по песням юзера</param>
        /// <param name="offset">Смещение</param>
        public List<Audio> AudioSearch(string searchString, int count = 10, bool autoComplete = true,
            bool lyrics = false, bool performerOnly = false, int sort = 2,bool searchOwn = false,
            int offset = 0)
        {
            var parameters = new NameValueCollection();

            parameters["q"] = searchString;
            if (count != 30) //По дефолту 30 песен.
                parameters["count"] = count.ToString();
            if (autoComplete == true)
                parameters["auto_complete"] = 1.ToString();
            if (lyrics == true)
                parameters["lyrics"] = 1.ToString();
            if (performerOnly == true)
                parameters["performer_only"] = 1.ToString();
            if (sort != 2)
                parameters["sort"] = sort.ToString();
            if (searchOwn == true)
                parameters["search_own"] = 1.ToString();
            if (offset != 0)
                parameters["offset"] = offset.ToString();

            XmlDocument audioSearchXml = ExecuteCommand("audio.search", parameters);
            return parseXmlToAudios(audioSearchXml);
        }

        /// <summary>
        /// Выдать рекомендованные аудиозаписи.
        /// </summary>
        /// <param name="count">Количество</param>
        /// <param name="shuffle">Рандом</param>
        /// <param name="offset">Смещение</param>
        /// <param name="userId">Ид юзера</param>
        /// <param name="targetAudio">На основе этой песни</param>
        public List<Audio> AudioGetRecommendations(int count = 100, bool shuffle = false, int offset = 0, int? userId = null,  string targetAudio = "")
        {
            var parameters = new NameValueCollection();
            List<Audio> audioList;

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
            audioList = parseXmlToAudios(recomendAudiosXml);

            if (!shuffle)
                return audioList.OrderBy(x => x.Artist).ThenBy(x => x.Title).ToList();
            return audioList;
        }

        /// <summary>
        /// Добавить песню себе в аудизаписи или группу.
        /// </summary>
        /// <param name="audioId">Ид песни</param>
        /// <param name="ownerId">Владелец песни</param>
        /// <param name="groupId">Добавить в Id_сообствество</param>
        public void AudioAdd(uint audioId, int ownerId, int groupId = -1)
        {
            var parameters = new NameValueCollection();

            parameters["audio_id"] = audioId.ToString();

            parameters["owner_id"] = ownerId.ToString();
            //Отправляем только отличные от стандартных параметры.
            if (groupId != -1)
                parameters["group_id"] = groupId.ToString();

            XmlDocument result = ExecuteCommand("audio.add", parameters);

        }

        //Извечение данных из структуры вида:
        //<response>
        //  <count>400</count>
        //  <items list="true">
        //      <audio>
        //      </audio>
        //      <audio>
        private List<Audio> parseXmlToAudios(XmlDocument xmlDocument)
        {
            var audioList = new List<Audio>();
            XmlNode reNode = xmlDocument.SelectSingleNode("response");
            if (reNode == null)
            {
                var errNode = xmlDocument.SelectSingleNode("error");
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
                    curAudio.GenreId = (AudioGenre)Convert.ToUInt32(genreIdNode.InnerText);

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
