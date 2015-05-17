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
        public void ReplaceWithBetterQuality(Audio audioToCompare)
        {
            if (audioToCompare.Kbps >= 315)
                return;
            Audio replacedAudio = new Audio();
            var finded = _vkApi.AudioSearch(audioToCompare.GetArtistDashTitle());
            CalcKbps(finded);
            foreach (var audioFinded in finded)
            {
                if (Math.Abs(audioToCompare.Duration - audioFinded.Duration) > 5)
                    continue;
                if (audioFinded.Kbps >= 315)
                {
                    audioToCompare = audioFinded;
                    return;
                }

                if (audioToCompare.Kbps < audioFinded.Kbps)
                    replacedAudio = audioFinded;
            }
            audioToCompare = replacedAudio;
        }

        public List<Audio> GetRecommendations(int count, bool random = false, int offset = 0)
        {
            var audios =  _vkApi.AudioGetRecommendations(count, random, offset);
            CalcKbps(audios);
            return audios;
        }

        private void CalcKbps(List<Audio> audios)
        {
            List<Audio> newAudios;
            foreach (var audio in audios)
            {
                WebRequest request = HttpWebRequest.Create(audio.Url);
                request.Method = "HEAD";
                using (WebResponse response = request.GetResponse())
                {
                    long contentLength;
                    if (long.TryParse(response.Headers.Get("Content-Length"), out contentLength))
                    {
                        audio.Kbps = (int)((contentLength * 8 / 1024) / audio.Duration);
                    }
                }
            }
        }
    }
}
