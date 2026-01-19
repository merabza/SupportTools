using System.Net.Http;
using CliParameters;
using CliParameters.FieldEditors;
using CliParametersDataEdit.FieldEditors;
using CliParametersEdit.FieldEditors;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.FieldEditors;
using SupportToolsData.Models;

namespace SupportTools.ParametersEditors;

public sealed class AppProjectCreatorParametersEditor : ParametersEditor
{
    public AppProjectCreatorParametersEditor(ILogger logger, IHttpClientFactory httpClientFactory,
        IParameters parameters, IParametersManager parametersManager, ParametersManager listsParametersManager) : base(
        "AppProjectCreatorParametersEditor", parameters, parametersManager)
    {
        FieldEditors.Add(new IntFieldEditor(nameof(AppProjectCreatorAllParameters.IndentSize)));
        FieldEditors.Add(new TextFieldEditor(nameof(AppProjectCreatorAllParameters.FakeHostProjectName)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(AppProjectCreatorAllParameters.ProjectsFolderPathReal)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(AppProjectCreatorAllParameters.SecretsFolderPathReal)));
        FieldEditors.Add(new DatabaseServerConnectionNameFieldEditor(logger, httpClientFactory,
            nameof(AppProjectCreatorAllParameters.DeveloperDbConnectionName), listsParametersManager, true));
        //გაცვლის ფაილსაცავის სახელი
        FieldEditors.Add(new FileStorageNameFieldEditor(logger,
            nameof(AppProjectCreatorAllParameters.DatabaseExchangeFileStorageName), listsParametersManager));
        //ჭკვიანი სქემის სახელი.
        FieldEditors.Add(new SmartSchemaNameFieldEditor(nameof(AppProjectCreatorAllParameters.UseSmartSchema),
            listsParametersManager));
        FieldEditors.Add(new ServerDataNameFieldEditor(logger, httpClientFactory,
            nameof(AppProjectCreatorAllParameters.ProductionServerName), listsParametersManager));
        FieldEditors.Add(new EnvironmentNameFieldEditor(
            nameof(AppProjectCreatorAllParameters.ProductionEnvironmentName), listsParametersManager));
    }
}