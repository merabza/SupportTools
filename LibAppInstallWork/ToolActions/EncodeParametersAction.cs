using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibToolActions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SystemToolsShared;
using SystemToolsShared.Domain;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolActions;

public sealed class EncodeParametersAction : ToolAction
{
    private readonly string _encodedJsonFileName;
    private readonly string _keyPart1;
    private readonly string _keyPart2;
    private readonly string _keysJsonFileName;
    private readonly ILogger _logger;
    private readonly string _sourceJsonFileName;

    public EncodeParametersAction(ILogger logger, string keysJsonFileName, string sourceJsonFileName,
        string encodedJsonFileName, string keyPart1, string keyPart2) : base(logger, "Encode Parameters", null, null)
    {
        _logger = logger;
        _sourceJsonFileName = sourceJsonFileName;
        _encodedJsonFileName = encodedJsonFileName;
        _keysJsonFileName = keysJsonFileName;
        _keyPart1 = keyPart1;
        _keyPart2 = keyPart2;
    }

    public string? AppSettingsVersion { get; private set; }

    public string? EncodedJsonContent { get; private set; }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        EncodedJsonContent = CreateEncodedJson();
        var success = false;
        if (EncodedJsonContent != null)
        {
            File.WriteAllText(_encodedJsonFileName, EncodedJsonContent);
            success = true;
        }

        if (!success)
            _logger.LogWarning("Encoded file does not created");
        return Task.FromResult(success);
    }


    private string? CreateEncodedJson()
    {
        //Get Whole json file and change only passed key with passed value.
        //It requires modification if you need to support change multi level json structure

        if (string.IsNullOrWhiteSpace(_keysJsonFileName))
        {
            _logger.LogError("keys file is not specified");
            return null;
        }

        if (!File.Exists(_keysJsonFileName))
        {
            _logger.LogError("keys file {_keysJsonFileName} is not exists", _keysJsonFileName);
            return null;
        }

        //string appSetEnKeysJsonString = File.ReadAllText(_keysJsonFileName);
        //KeysList? appSetEnKeysList = JsonConvert.DeserializeObject<KeysList>(appSetEnKeysJsonString);


        var appSetEnKeysList = KeysListDomain.LoadFromFile(_keysJsonFileName);
        if (appSetEnKeysList?.Keys is null)
            return null;


        //string encKey = $"{_keyPart1 ?? ""}{_keyPart2.Capitalize() ?? ""}";
        var encKey = $"{_keyPart1}{_keyPart2.Capitalize()}";

        if (encKey == "")
        {
            _logger.LogError("key is not defined");
            return null;
        }

        var appSetJson = File.ReadAllText(_sourceJsonFileName);
        var appSetJObject = JObject.Parse(appSetJson);

        //დავადგინოთ _appsetenParameters._sourceJsonFileName ფაილის ფოლდერის სახელი
        var directoryName = Path.GetDirectoryName(_sourceJsonFileName);

        if (directoryName != null)
        {
            //დავადგინოთ ამ ფოლდერში არის თუ არა csproj ფაილი
            var dir = new DirectoryInfo(directoryName);
            var csprojFile = dir.GetFiles().SingleOrDefault(w => Path.GetExtension(w.Name) == ".csproj");

            //თუ არსებობს გავხსნათ ეს csproj ფაილი და წავიკითხოთ როგორც XLM 
            if (csprojFile != null)
            {
                var userSecretContentFileName = UserSecretFileNameDetector.GetFileName(csprojFile.FullName);

                if (userSecretContentFileName is not null && File.Exists(userSecretContentFileName))
                {
                    var secretJson = File.ReadAllText(userSecretContentFileName);
                    var secretJObject = JObject.Parse(secretJson);

                    // შევაერთოთ jsonObj ობიექტთან
                    appSetJObject.Merge(secretJObject, new JsonMergeSettings
                    {
                        // union array values together to avoid duplicates
                        MergeArrayHandling = MergeArrayHandling.Merge
                    });
                }
            }
        }

        //var recountedKeys = new List<string>();
        //foreach (var dataKey in appSetEnKeysList.Keys)
        //{
        //    var keys = dataKey.Split(":");
        //    if (keys.Length == 0)
        //        continue;
        //    if (keys.Contains("[]") || keys.Contains("*")) 
        //        recountedKeys.AddRange(RecountKeys(appSetJObject, keys));
        //    else
        //        recountedKeys.Add(dataKey);
        //}


        foreach (var dataKey in appSetEnKeysList.Keys.Select(dataKey => new { dataKey, keys = dataKey.Split(":") })
                     .Where(w => w.keys.Length != 0)
                     .Where(w => !Enc(appSetJObject, encKey, w.keys))
                     .Select(s => s.dataKey))
            _logger.LogWarning("cannot found dataKey {dataKey}", dataKey);

        AppSettingsVersion = DateTime.Now.ToString(CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(appSetJObject["VersionInfo"]?["AppSettingsVersion"]?.Value<string>()))
            StShared.WriteWarningLine(
                $"AppSettingsVersion did not defined. If you continue, we can not check installed AppSettingsVersion. Please add to {_sourceJsonFileName} file VersionInfo:AppSettingsVersion",
                true, _logger, true);

        appSetJObject["VersionInfo"]?["AppSettingsVersion"]?.Replace(AppSettingsVersion);
        return JsonConvert.SerializeObject(appSetJObject, Formatting.Indented);
    }

    //private List<string> RecountKeys(JToken? val, string[] keys, int k = 0)
    //{

    //    if (val is null)
    //        return new List<string>();

    //    if (k == keys.Length)
    //        return new List<string> { string.Join(":", keys) };

    //    switch (keys[k])
    //    {
    //        case "[]":
    //            return CountKList(val, keys, k);
    //        //return val.All(v => Enc(v, encKey, keys, k + 1));
    //        case "*":
    //            var kList = new List<string>();
    //            foreach (var value in val.Values())
    //            {
    //                kList.AddRange(CountKListAst(value, keys, k));
    //            }
    //            return kList;
    //    }
    //    var byKi = int.TryParse(keys[k], out var ki);

    //    var valueByKey = byKi ? val[ki] : val[keys[k]];

    //    return valueByKey == null ? new List<string>() : RecountKeys(valueByKey, keys, k + 1);
    //}

    //private List<string> CountKListAst(JToken val, string[] keys, int k)
    //{
    //    var kList = new List<string>();

    //    foreach (var v in val)
    //    {
    //        Console.WriteLine(v.Path);
    //    }


    //    for (var i = 0; i < val.Length(); i++)
    //    {
    //        var newKeys = new List<string>();
    //        for (var j = 0; j < k; j++)
    //            newKeys.Add(keys[j]);
    //        newKeys.Add(i.ToString());
    //        for (var j = k + 1; j < keys.Length; j++)
    //            newKeys.Add(keys[j]);
    //        kList.AddRange(RecountKeys(val[i], newKeys.ToArray(), k + 1));
    //    }

    //    return kList;
    //}

    //private List<string> CountKList(JToken val, string[] keys, int k)
    //{
    //    var kList = new List<string>();

    //    for (var i = 0; i < val.Length(); i++)
    //    {
    //        var newKeys = new List<string>();
    //        for (var j = 0; j < k; j++)
    //            newKeys.Add(keys[j]);
    //        newKeys.Add(i.ToString());
    //        for (var j = k + 1; j < keys.Length; j++)
    //            newKeys.Add(keys[j]);
    //        kList.AddRange(RecountKeys(val[i], newKeys.ToArray(), k + 1));
    //    }

    //    return kList;
    //}

    private static bool Enc(JToken val, string encKey)
    {
        if (val.Type != JTokenType.String)
            return val.All(v => Enc(v, encKey));
        var value = val.Value<string>();
        if (value is not null)
            val.Replace(EncryptDecrypt.EncryptString(value, encKey));
        return true;
    }

    private static bool Enc(JToken val, string encKey, string[] keys, int k = 0)
    {
        if (k == keys.Length)
            return Enc(val, encKey);

        switch (keys[k])
        {
            case "[]":
                var encodedPaths = new List<string>();

                var atLastOneEncoded = true;
                while (atLastOneEncoded)
                {
                    atLastOneEncoded = false;
                    foreach (var v in val)
                    {
                        var path = v.Path;
                        if (encodedPaths.Contains(path))
                            continue;
                        if (!Enc(v, encKey, keys, k + 1))
                            return false;
                        encodedPaths.Add(path);
                        atLastOneEncoded = true;
                        break;
                    }
                }

                return true;
            case "*":
                return val.Values().All(p => Enc(p, encKey, keys, k + 1));
        }

        var byKi = int.TryParse(keys[k], out var ki);

        var valueByKey = byKi ? val[ki] : val[keys[k]];

        return valueByKey == null || Enc(valueByKey, encKey, keys, k + 1);
    }
}