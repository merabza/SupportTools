//Created by ProjectMainClassCreator at 12/22/2020 19:46:17

using CliParameters;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ProgramPublisher : ToolCommand
{
    private const string ActionName = "Publishing App";
    private const string ActionDescription = "Publishing App";
    private readonly ProgramPublisherParameters _parameters;

    public ProgramPublisher(ILogger logger, ProgramPublisherParameters parameters, IParametersManager parametersManager)
        : base(logger, ActionName, parameters, parametersManager, ActionDescription)
    {
        _parameters = parameters;
    }

    protected override bool RunAction()
    {
        //1. შევქმნათ საინსტალაციო პაკეტი და ავტვირთოთ ფაილსაცავში
        var createPackageAndUpload = new CreatePackageAndUpload(Logger, _parameters.ProjectName,
            _parameters.MainProjectFileName, _parameters.ServerInfo, _parameters.WorkFolder, _parameters.DateMask,
            _parameters.Runtime, _parameters.RedundantFileNames, _parameters.UploadTempExtension,
            _parameters.FileStorageForExchange, _parameters.SmartSchemaForLocal, _parameters.SmartSchemaForExchange);
        return createPackageAndUpload.Run();
    }
}