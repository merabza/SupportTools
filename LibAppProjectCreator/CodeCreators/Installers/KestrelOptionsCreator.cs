//Created by CreatorClassCreator at 7/2/2022 8:50:49 PM

using Newtonsoft.Json.Linq;

namespace LibAppProjectCreator.CodeCreators.Installers;

public sealed class KestrelOptionsCreator
{
    private readonly JObject _appSettingsJsonJObject;

    public KestrelOptionsCreator(JObject appSettingsJsonJObject)
    {
        _appSettingsJsonJObject = appSettingsJsonJObject;
    }


    public void Run()
    {
        _appSettingsJsonJObject.Add(new JProperty("Kestrel", new JObject(
            new JProperty("Endpoints", new JObject(
                new JProperty("Http", new JObject(
                    new JProperty("Url", "http://*:5099"))))))));
    }
}