//Created by CreatorClassCreator at 7/2/2022 8:50:49 PM

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LibAppProjectCreator.CodeCreators.Installers;

public sealed class IdentitySettingsCreator
{
    private readonly JObject _appSettingsJsonJObject;
    private readonly List<string> _forEncodeAppSettingsJsonKeys;
    private readonly JObject _userSecretJsonJObject;

    // ReSharper disable once ConvertToPrimaryConstructor
    public IdentitySettingsCreator(JObject appSettingsJsonJObject, JObject userSecretJsonJObject,
        List<string> forEncodeAppSettingsJsonKeys)
    {
        _appSettingsJsonJObject = appSettingsJsonJObject;
        _userSecretJsonJObject = userSecretJsonJObject;
        _forEncodeAppSettingsJsonKeys = forEncodeAppSettingsJsonKeys;
    }

    public void Run()
    {
        _appSettingsJsonJObject.Add(new JProperty("IdentitySettings",
            new JObject(new JProperty("JwtSecret", "JwtSecret"))));

        _userSecretJsonJObject.Add(new JProperty("IdentitySettings",
            new JObject(new JProperty("JwtSecret", Guid.NewGuid().ToString("N")))));

        _forEncodeAppSettingsJsonKeys.Add("IdentitySettings:JwtSecret");
    }
}