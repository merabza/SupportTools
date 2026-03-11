using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Domain;
using ToolsManagement.LibToolActions;

namespace LibAppInstallWork.ToolActions;

public sealed class EncodeParametersAction : ToolAction
{
    private readonly string _encodedJsonFileName;
    private readonly string _keyPart1;
    private readonly string _keyPart2;
    private readonly string _keysJsonFileName;
    private readonly ILogger _logger;
    private readonly string _sourceJsonFileName;

    // ReSharper disable once ConvertToPrimaryConstructor
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

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        EncodedJsonContent = CreateEncodedJson();
        bool success = false;
        if (EncodedJsonContent != null)
        {
            await File.WriteAllTextAsync(_encodedJsonFileName, EncodedJsonContent, cancellationToken);
            success = true;
        }

        if (!success)
        {
            _logger.LogWarning("Encoded file does not created");
        }

        return success;
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

        KeysListDomain? appSetEnKeysList = KeysListDomain.LoadFromFile(_keysJsonFileName);
        if (appSetEnKeysList?.Keys is null)
        {
            return null;
        }

        string encKey = $"{_keyPart1}{_keyPart2.Capitalize()}";

        if (string.IsNullOrEmpty(encKey))
        {
            _logger.LogError("key is not defined");
            return null;
        }

        string appSetJson = File.ReadAllText(_sourceJsonFileName);
        JObject appSetJObject = JObject.Parse(appSetJson);

        //დავადგინოთ _appsetenParameters._sourceJsonFileName ფაილის ფოლდერის სახელი
        string? directoryName = Path.GetDirectoryName(_sourceJsonFileName);

        if (directoryName != null)
        {
            //დავადგინოთ ამ ფოლდერში არის თუ არა csproj ფაილი
            var dir = new DirectoryInfo(directoryName);
            FileInfo? csprojFile = dir.GetFiles().SingleOrDefault(w => Path.GetExtension(w.Name) == ".csproj");

            //თუ არსებობს გავხსნათ ეს csproj ფაილი და წავიკითხოთ როგორც XLM 
            if (csprojFile != null)
            {
                string? userSecretContentFileName = UserSecretFileNameDetector.GetFileName(csprojFile.FullName);

                if (userSecretContentFileName is not null && File.Exists(userSecretContentFileName))
                {
                    string secretJson = File.ReadAllText(userSecretContentFileName);
                    JObject secretJObject = JObject.Parse(secretJson);

                    // შევაერთოთ jsonObj ობიექტთან
                    appSetJObject.Merge(secretJObject, new JsonMergeSettings
                    {
                        // union array values together to avoid duplicates
                        MergeArrayHandling = MergeArrayHandling.Merge
                    });
                }
            }
        }

        foreach (string dataKey in appSetEnKeysList.Keys.Select(dataKey => new { dataKey, keys = dataKey.Split(":") })
                     .Where(w => w.keys.Length != 0).Where(w => !Enc(appSetJObject, encKey, w.keys))
                     .Select(s => s.dataKey))
        {
            _logger.LogWarning("cannot found dataKey {DataKey}", dataKey);
        }

        AppSettingsVersion = DateTime.Now.ToString(CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(appSetJObject["VersionInfo"]?["AppSettingsVersion"]?.Value<string>()))
        {
            StShared.WriteWarningLine(
                $"AppSettingsVersion did not defined. If you continue, we can not check installed AppSettingsVersion. Please add to {_sourceJsonFileName} file VersionInfo:AppSettingsVersion",
                true, _logger, true);
        }

        appSetJObject["VersionInfo"]?["AppSettingsVersion"]?.Replace(AppSettingsVersion);
        return JsonConvert.SerializeObject(appSetJObject, Formatting.Indented);
    }

    private static bool Enc(JToken val, string encKey)
    {
        if (val.Type != JTokenType.String)
        {
            return val.All(v => Enc(v, encKey));
        }

        string? value = val.Value<string>();
        if (value is not null)
        {
            val.Replace(EncryptDecrypt.EncryptString(value, encKey));
        }

        return true;
    }

    private static bool Enc(JToken val, string encKey, string[] keys, int k = 0)
    {
        if (k == keys.Length)
        {
            return Enc(val, encKey);
        }

        switch (keys[k])
        {
            case "[]":
                var encodedPaths = new List<string>();

                bool atLastOneEncoded = true;
                while (atLastOneEncoded)
                {
                    atLastOneEncoded = false;
                    foreach (JToken v in val)
                    {
                        string path = v.Path;
                        if (encodedPaths.Contains(path))
                        {
                            continue;
                        }

                        if (!Enc(v, encKey, keys, k + 1))
                        {
                            return false;
                        }

                        encodedPaths.Add(path);
                        atLastOneEncoded = true;
                        break;
                    }
                }

                return true;
            case "*":
                return val.Values().All(p => Enc(p, encKey, keys, k + 1));
        }

        bool byKi = int.TryParse(keys[k], out int ki);

        JToken? valueByKey = byKi ? val[ki] : val[keys[k]];

        return valueByKey == null || Enc(valueByKey, encKey, keys, k + 1);
    }
}
