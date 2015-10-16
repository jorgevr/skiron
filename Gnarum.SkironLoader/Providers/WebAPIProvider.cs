using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gnarum.SkironLoader.Model.Entities;
using Gnarum.WebApiCommon;

namespace Gnarum.SkironLoader.Providers
{
    public interface IWebAPIProvider
    {
        ApiPlant GetPlant(string plant);
        ApiDatavariable GetDatavariable(string datavariable);
        bool PutWeatherForecastList(string json);
        ApiWeatherModel GetApiWeatherModel(string idWeatherModel);

    }

    public class WebAPIProvider : IWebAPIProvider
    {
        string _webApiURL;

        public WebAPIProvider(string webApiURL)
        {
            _webApiURL = webApiURL;
        }




        public ApiPlant GetPlant(string idPlant)
        {
            return WebApiCommonUtil.Get<ApiPlant>(string.Format("/api/plant/{0}", idPlant), _webApiURL);
        }

        public ApiDatavariable GetDatavariable(string idDatavariable)
        {
            return WebApiCommonUtil.Get<ApiDatavariable>(string.Format("/api/datavariable/{0}", idDatavariable), _webApiURL);
        }

        public ApiWeatherModel GetApiWeatherModel(string idWeatherModel)
        {
            return WebApiCommonUtil.Get<ApiWeatherModel>(string.Format("/api/weathermodel/{0}", idWeatherModel), _webApiURL);
        }

        /// <summary>
        /// Realiza una llamada a la Web API para enviar mediante PUT una lista de Measure en formato JSON
        /// </summary>
        /// <param name="json">Cadena con lista de Measure en formato JSON</param>
        /// <returns><c>true</c> si se produjo en envío con éxito o <c>false</c> en caso contrario</returns>
        public bool PutWeatherForecastList(string json)
        {
            return WebApiCommonUtil.Put(_webApiURL, "/api/weatherforecast", json);
        }
    }
}
