using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

//წყვილების ფაილის აღდგენა მისი ერთ-ერთი ბექაპიდან. აღდგენაც Save-ით ხდება, ამიტომ მიმდინარე ფაილი თავის მხრივ
//ბექაპად რჩება და აღდგენის გაუქმებაც შეიძლება
public sealed class RestorePairedDbObjectsFromBackupCliMenuCommand : CliMenuCommand
{
    private readonly Func<string, bool, bool> _inputBool;
    private readonly ILogger _logger;
    private readonly string _resultFileName;
    private readonly Func<string, List<string>, string?> _selectBackup;

    public RestorePairedDbObjectsFromBackupCliMenuCommand(ILogger logger, string resultFileName) : this(logger,
        resultFileName, SelectBackup, (caption, defaultValue) => Inputer.InputBool(caption, defaultValue, false))
    {
    }

    //კონსოლიდან შეყვანა პარამეტრებადაა გამოტანილი, რომ ტესტებმა პასუხები თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal RestorePairedDbObjectsFromBackupCliMenuCommand(ILogger logger, string resultFileName,
        Func<string, List<string>, string?> selectBackup, Func<string, bool, bool> inputBool) : base(
        "Restore Paired Db Objects From Backup...", EMenuAction.Reload)
    {
        _logger = logger;
        _resultFileName = resultFileName;
        _selectBackup = selectBackup;
        _inputBool = inputBool;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        List<string> backupFiles = PairedDbObjectsBackups.GetBackupFiles(_resultFileName);
        if (backupFiles.Count == 0)
        {
            StShared.WriteErrorLine($"No backups of {_resultFileName} found", true, _logger);
            return false;
        }

        List<string> backupNames = [.. backupFiles.Select(Path.GetFileName).OfType<string>()];
        string? backupName = _selectBackup("Backup to restore (newest first)", backupNames);
        int index = backupName is null ? -1 : backupNames.IndexOf(backupName);
        if (index < 0)
        {
            return false;
        }

        string backupFile = backupFiles[index];

        PairedDbObjectsModel? model;
        try
        {
            string json = await File.ReadAllTextAsync(backupFile, cancellationToken);
            model = JsonConvert.DeserializeObject<PairedDbObjectsModel>(json);
        }
        catch (JsonException ex)
        {
            StShared.WriteException(ex, $"Backup {backupFile} is not a paired DB objects file", true, _logger);
            return false;
        }

        if (model is null)
        {
            StShared.WriteErrorLine($"Backup {backupFile} is empty", true, _logger);
            return false;
        }

        if (!_inputBool(
                $"Replace {_resultFileName} with backup {backupName} ({model.PairedTables.Count} paired tables)? The current file is kept as a backup",
                false))
        {
            return false;
        }

        var parametersManager = new PairedDbObjectsParametersManager(_resultFileName, model);
        return await parametersManager.Save(model, $"Paired DB objects restored from {backupName}", null,
            cancellationToken);
    }

    //Escape-ით გასვლა შეცდომაა და არა არჩევანი
    private static string? SelectBackup(string caption, List<string> backupNames)
    {
        var menuSet = new CliMenuSet();
        foreach (string backupName in backupNames)
        {
            menuSet.AddMenuItem(new CliMenuCommand(backupName));
        }

        try
        {
            return MenuInputer.InputFromMenuList(caption, menuSet);
        }
        catch (DataInputException)
        {
            return null;
        }
    }
}
