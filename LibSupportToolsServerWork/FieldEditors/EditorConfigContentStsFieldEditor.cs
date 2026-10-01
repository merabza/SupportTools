using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibSupportToolsServerWork.FieldEditors;

//.editorconfig შაბლონის შინაარსი ფაილიდან იტვირთება: ათასობით სიმბოლოს კონსოლში აკრეფა პრაქტიკული არ არის
public sealed class EditorConfigContentStsFieldEditor : FieldEditor<string>
{
    private readonly Func<string, string?, string?> _inputFilePath;
    private readonly IParametersManager _parametersManager;

    public EditorConfigContentStsFieldEditor(string propertyName, IParametersManager parametersManager,
        bool enterFieldDataOnCreate = false) : this(propertyName, parametersManager, enterFieldDataOnCreate,
        (fieldName, defaultValue) => MenuInputer.InputFilePath(fieldName, defaultValue, false))
    {
    }

    //ფაილის გზის შეტანა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal EditorConfigContentStsFieldEditor(string propertyName, IParametersManager parametersManager,
        bool enterFieldDataOnCreate, Func<string, string?, string?> inputFilePath) : base(propertyName,
        enterFieldDataOnCreate, propertyDescriptor: "Content")
    {
        _parametersManager = parametersManager;
        _inputFilePath = inputFilePath;
    }

    public override async ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        string? fileName = _inputFilePath($"{FieldName} Source File", GetDefaultFileName(recordKey));
        if (!File.Exists(fileName))
        {
            throw new DataInputException($"File {fileName} does not exist");
        }

        SetValue(recordForUpdate, await File.ReadAllTextAsync(fileName, cancellationToken));
    }

    public override string GetValueStatus(object? record)
    {
        string? content = GetValue(record);
        return content is null ? string.Empty : $"{content.Length} characters";
    }

    //შემოთავაზებული ფაილი შაბლონების ფოლდერიდანაა: არსებული ჩანაწერისთვის იგივე სახელის შაბლონი,
    //ახალი ჩანაწერისთვის (მისი სახელი ველს არ გადმოეცემა) თვითონ ფოლდერი
    private string? GetDefaultFileName(string? recordKey)
    {
        string? folderForEditorConfigFiles =
            ((SupportToolsParameters)_parametersManager.Parameters).FolderForEditorConfigFiles;
        if (string.IsNullOrWhiteSpace(folderForEditorConfigFiles))
        {
            return null;
        }

        return recordKey is null
            ? folderForEditorConfigFiles.AddNeedLastPart(Path.DirectorySeparatorChar)
            : SupportToolsParameters.GetEditorConfigPatternFilePath(folderForEditorConfigFiles, recordKey);
    }
}
