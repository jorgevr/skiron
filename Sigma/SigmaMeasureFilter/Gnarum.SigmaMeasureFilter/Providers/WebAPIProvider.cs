using System.Collections.Generic;
using Gnarum.SigmaMeasureFilter.Model.Entities;
using Gnarum.WebApiCommon;

namespace Gnarum.SigmaMeasureFilter.Providers
{
    public interface IWebAPIProvider
    {
        IList<Plant> GetAllActiveWithSigmaConditionFromURL(string URL);

        bool PutJSONMeasureList(string URL, string json);
    }

    class WebAPIProvider : IWebAPIProvider
    {
        public IList<Plant> GetAllActiveWithSigmaConditionFromURL(string URL)
        {
            return WebApiCommonUtil.Get<IList<Plant>>("/api/plant?measuresource=SIGMA", URL);
        }

        public bool PutJSONMeasureList(string URL, string json)
        {
            return WebApiCommonUtil.Put(URL, "/api/measure", json);
        }
    }
}
