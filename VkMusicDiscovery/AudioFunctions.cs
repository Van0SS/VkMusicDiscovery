using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VkMusicDiscovery
{
    public class AudioFunctions
    {
        private VkApi _vkApi;

        public AudioFunctions(string token, int userId)
        {
            _vkApi = new VkApi(token, userId);
        }

        //Но пока скорей всего лучшей картинкой...
        public Audio ReplaceWithBetterQuality(Audio audioToCompare)
        {
            var lengthDifference = 5; //Разница в 5 сек не существенна, скорей всего та же песня
            //var enoughQuality = 315; //315 из 320 kbps вполне для mp3
            var findSongCount = 10; //10 первые найденных песен хватит для анализа, и не сильно долго

            calsAudioKbps(audioToCompare);
            /*if (audioToCompare.Kbps >= enoughQuality) //Ушам хватит
            {
                return audioToCompare;
            }*/

            //Перед сравнением берём за основу текущую песню, потом заменяем её той, у которой лучше качество
            Audio replacedAudio = audioToCompare;
            var finded = _vkApi.AudioSearch(audioToCompare.GetArtistDashTitle(), findSongCount);

            finded = deleteAnotherNameAudios(audioToCompare, finded);

            if (finded.Count == 0)
                return audioToCompare;

            calsManyAudiosKbps(finded); //Считаем kbps найденных песен
            foreach (var audioFinded in finded)
            {
                //Сравнение длин песен
                if (Math.Abs(audioToCompare.Duration - audioFinded.Duration) > lengthDifference)
                    continue;
                /* if (audioFinded.Kbps >= enoughQuality)
                 {
                     return audioToCompare;
                 }*/

                //Если у заменяемой песни хуже качество, то заменяем найденной
                if (replacedAudio.Kbps < audioFinded.Kbps)
                {
                    audioToCompare = audioFinded;
                }
            }
            return audioToCompare;
        }

        public List<Audio> GetRecommendations(int count, bool random = false, int offset = 0)
        {
            var audios =  _vkApi.AudioGetRecommendations(count, random, offset);
            return audios;
        }

        public void AddAudioToSongs(List<Audio> audios)
        {
            foreach (var audio in audios)
            {
                _vkApi.AudioAdd(audio.Id, audio.OwnerId);
            }
        }

        #region - Private methods -

        /// <summary>
        /// Оставить в листе песни, только у которых совпадает название.
        /// </summary>
        /// <param name="audio">Искомая песня</param>
        /// <param name="findedAudios">Сравниваемые песени</param>
        /// <returns></returns>
        private List<Audio> deleteAnotherNameAudios(Audio audio, List<Audio> findedAudios)
        {
            string nameLowerCase = audio.GetArtistDashTitle().ToLowerInvariant();
            var songsAfterDelete = new List<Audio>();
            foreach (var findedAudio in findedAudios)
            {
                if (nameLowerCase == findedAudio.GetArtistDashTitle().ToLowerInvariant())
                    songsAfterDelete.Add(findedAudio);
            }
            return songsAfterDelete;
        }

        private void calsAudioKbps(Audio audio)
        {
            if (audio.Kbps != 0)
                return;
            WebRequest request = HttpWebRequest.Create(audio.Url);
            request.Method = "HEAD";
            //Запрашиваем заголовок файла
            using (WebResponse response = request.GetResponse())
            {
                long contentLength;
                //Берём из заголовка mp3 параметр размера файла
                if (long.TryParse(response.Headers.Get("Content-Length"), out contentLength))
                {
                    //Определиние kbps и качества картинки методом деления размера файла на длину песни
                    audio.Kbps = (int)((contentLength * 8 / 1024) / audio.Duration); //TODO сделать считывание первых байт заголовка мп3
                }
            }
        }

        //Перегрузка не удалась
        private void calsManyAudiosKbps(IList<Audio> audios)
        {
            foreach (var audio in audios)
            {
                calsAudioKbps(audio);
            }
        }

        #endregion - Private methods -
    }
}
