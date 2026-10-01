using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.Cruders;
using LibSupportToolsServerWork.FieldEditors;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace LibSupportToolsServerWork.Cruders;

//სერვერზე შენახული .editorconfig შაბლონები. ჩანაწერის TextItemData.Text მისი შინაარსია.
//სერვერი ჩანაწერს სახელით ადარებს, ამიტომ დამატებაც და შეცვლაც ერთი ჩანაწერის sync-up-ია merge=true-თი,
//რომელიც სხვა ჩანაწერებს არ ეხება
public sealed class EditorConfigFileTypesStsCruder : Cruder
{
    private readonly EditorConfigContentStsFieldEditor _contentFieldEditor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    //სერვერიდან ჩამოტვირთული სია. სერვერზე ყოველი ცვლილების შემდეგ თავიდან ჩამოიტვირთება
    private List<StsEditorConfigFileTypeDataModel>? _serverRecords;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EditorConfigFileTypesStsCruder(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : base("EditorConfig File Type", "EditorConfig File Types")
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _contentFieldEditor = new EditorConfigContentStsFieldEditor(nameof(TextItemData.Text), parametersManager, true);
        FieldEditors.Add(_contentFieldEditor);
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return GetServerRecords().ToDictionary(k => k.Name, ItemData (v) => new TextItemData { Text = v.Content },
            StringComparer.OrdinalIgnoreCase);
    }

    //სახელი სხვა რეგისტრითაც დაკავებულია: ასეთი ჩანაწერის ატვირთვა არსებულს გადააწერდა
    public override bool ContainsRecordWithKey(string recordKey)
    {
        return GetServerRecords().Exists(x => string.Equals(x.Name, recordKey, StringComparison.OrdinalIgnoreCase));
    }

    public override async ValueTask UpdateRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        SupportToolsServerApiClient? supportToolsServerApiClient = GetSupportToolsServerApiClient();
        if (supportToolsServerApiClient is null)
        {
            return;
        }

        var editorConfigFileType = new StsEditorConfigFileTypeDataModel
        {
            Name = recordKey, Content = ((TextItemData)newRecord).Text ?? string.Empty
        };
        Result uploadResult = await supportToolsServerApiClient.SyncUpEditorConfigFileTypes([editorConfigFileType],
            true, cancellationToken);
        _serverRecords = null;
        if (uploadResult.IsFailure)
        {
            PrintErrors(uploadResult.Error);
        }
    }

    protected override ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        return UpdateRecordWithKey(recordKey, newRecord, cancellationToken);
    }

    protected override async ValueTask RemoveRecordWithKey(string recordKey,
        CancellationToken cancellationToken = default)
    {
        SupportToolsServerApiClient? supportToolsServerApiClient = GetSupportToolsServerApiClient();
        if (supportToolsServerApiClient is null)
        {
            return;
        }

        Result removeResult =
            await supportToolsServerApiClient.RemoveEditorConfigFileTypeName(recordKey, cancellationToken);
        _serverRecords = null;
        if (removeResult.IsFailure)
        {
            PrintErrors(removeResult.Error);
        }
    }

    public override string GetStatusFor(string name)
    {
        return _contentFieldEditor.GetValueStatus(GetItemByName(name, false));
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new TextItemData();
    }

    private SupportToolsServerApiClient? GetSupportToolsServerApiClient()
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;

        return supportToolsParameters.GetSupportToolsServerApiClient(_logger, _httpClientFactory);
    }

    private List<StsEditorConfigFileTypeDataModel> GetServerRecords()
    {
        if (_serverRecords is not null)
        {
            return _serverRecords;
        }

        //სია GetSubMenu-დან იკითხება, რომელიც სინქრონულია. ApiClient ქსელის შეცდომას Result-ად აბრუნებს, მაგრამ
        //არასწორი პარამეტრები (სერვერის მისამართი, აპი კლიენტის სახელი) გამონაკლისს იწვევს, რომელიც GetSubMenu-დან
        //ამოვარდნისას მთელ პროგრამას დაასრულებდა
        Result<List<StsEditorConfigFileTypeDataModel>> serverRecordsResult;
        try
        {
            SupportToolsServerApiClient? supportToolsServerApiClient = GetSupportToolsServerApiClient();
            if (supportToolsServerApiClient is null)
            {
                return [];
            }

            serverRecordsResult = supportToolsServerApiClient.GetEditorConfigFileTypesList().Result;
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true, _logger);
            return [];
        }

        if (serverRecordsResult.IsFailure)
        {
            PrintErrors(serverRecordsResult.Error);
            return [];
        }

        _serverRecords = serverRecordsResult.Value;
        return _serverRecords;
    }

    //წაშლის, გადარქმევის და სიის ჩატვირთვის შემდეგ მენიუ პაუზის გარეშე იხატება და ეკრანს ასუფთავებს,
    //ამიტომ შეცდომის წასაკითხად პაუზაა საჭირო
    private static void PrintErrors(Error error)
    {
        error.PrintErrorsOnConsole();
        StShared.Pause();
    }
}
