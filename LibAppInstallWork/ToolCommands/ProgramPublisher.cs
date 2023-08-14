//Created by ProjectMainClassCreator at 12/22/2020 19:46:17

using CliParameters;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ProgramPublisher : ToolCommand
{
    private readonly ProgramPublisherParameters _parameters;
    private const string ActionName = "Publishing App";
    private const string ActionDescription = "Publishing App";

    public ProgramPublisher(ILogger logger, bool useConsole, ProgramPublisherParameters parameters,
        IParametersManager parametersManager) : base(logger, useConsole, ActionName, parameters, parametersManager,
        ActionDescription)
    {
        _parameters = parameters;
    }

    protected override bool RunAction()
    {
        //1. შევქმნათ საინსტალაციო პაკეტი და ავტვირთოთ ფაილსაცავში
        var createPackageAndUpload = new CreatePackageAndUpload(Logger, UseConsole,
            _parameters.ProjectName, _parameters.MainProjectFileName,
            _parameters.ServerInfo, _parameters.WorkFolder,
            _parameters.DateMask, _parameters.Runtime,
            _parameters.RedundantFileNames, _parameters.UploadTempExtension,
            _parameters.FileStorageForExchange, _parameters.SmartSchemaForLocal,
            _parameters.SmartSchemaForExchange);
        return createPackageAndUpload.Run();
    }
}