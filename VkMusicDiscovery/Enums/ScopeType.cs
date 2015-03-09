using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkMusicDiscovery.Enums
{
    /// <summary>
    /// Права доступа
    /// </summary>
    public enum ScopeType
    {
        /// <summary>
        /// Пользователь разрешил отправлять ему уведомления. 
        /// </summary>
        Notify = 1,
        /// <summary>
        /// Доступ к друзьям.
        /// </summary>
        Friends = 2,
        /// <summary>
        /// Доступ к фотографиям. 
        /// </summary>
        Photos = 4,
        /// <summary>
        /// Доступ к аудиозаписям. 
        /// </summary>
        Audio = 8,
        /// <summary>
        /// Доступ к видеозаписям. 
        /// </summary>
        Video = 16,
        /// <summary>
        /// Доступ к предложениям (устаревшие методы). 
        /// </summary>
        Offers = 32,
        /// <summary>
        /// Доступ к вопросам (устаревшие методы). 
        /// </summary>
        Questions = 64,
        /// <summary>
        /// Доступ к wiki-страницам. 
        /// </summary>
        Pages = 128,
        /// <summary>
        /// Добавление ссылки на приложение в меню слева.
        /// </summary>
        Link = 256,
        /// <summary>
        /// Доступ заметкам пользователя. 
        /// </summary>
        Notes = 2048,
        /// <summary>
        /// (для Standalone-приложений) Доступ к расширенным методам работы с сообщениями. 
        /// </summary>
        Messages = 4096,
        /// <summary>
        /// Доступ к обычным и расширенным методам работы со стеной. 
        /// </summary>
        Wall = 8192,
        /// <summary>
        /// Доступ к документам пользователя.
        /// </summary>
        Docs = 131072
    }

}
