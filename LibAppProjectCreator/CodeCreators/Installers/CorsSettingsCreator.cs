//Created by CreatorClassCreator at 7/2/2022 8:50:49 PM

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LibAppProjectCreator.CodeCreators.Installers;

public sealed class CorsSettingsCreator
{
    private readonly JObject _appSettingsJsonJObject;
    private readonly List<string> _forEncodeAppSettingsJsonKeys;
    private readonly string _projectNamespace;
    private readonly JObject _userSecretJsonJObject;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CorsSettingsCreator(string projectNamespace, JObject appSettingsJsonJObject, JObject userSecretJsonJObject,
        List<string> forEncodeAppSettingsJsonKeys)
    {
        _projectNamespace = projectNamespace;
        _appSettingsJsonJObject = appSettingsJsonJObject;
        _userSecretJsonJObject = userSecretJsonJObject;
        _forEncodeAppSettingsJsonKeys = forEncodeAppSettingsJsonKeys;
    }

    public void Run()
    {
        _appSettingsJsonJObject.Add(new JProperty("CorsSettings", new JObject(new JProperty("Origins", new JArray()))));

        _userSecretJsonJObject.Add(new JProperty("CorsSettings",
            new JObject(new JProperty("Origins", new JArray("http://localhost:5099")))));

        _forEncodeAppSettingsJsonKeys.Add("CorsSettings:Origins:[]");
    }
}