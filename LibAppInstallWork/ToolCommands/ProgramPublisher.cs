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

    public ProgramPublisher(ILogger logger, bool useConsole, ProgramPublisherParameters parameters,
        IParametersManager parametersManager) : base(logger, useConsole, ActionName, parameters, parametersManager,
        ActionDescription)
    {
    }

    private ProgramPublisherParameters ProgramPublisherParameters => (ProgramPublisherParameters)Par;

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        //1. შევქმნათ საინსტალაციო პაკეტი და ავტვირთოთ ფაილსაცავში
        var createPackageAndUpload = new CreatePackageAndUpload(Logger, UseConsole,
            ProgramPublisherParameters.ProjectName, ProgramPublisherParameters.MainProjectFileName,
            ProgramPublisherParameters.ServerInfo, ProgramPublisherParameters.WorkFolder,
            ProgramPublisherParameters.DateMask, ProgramPublisherParameters.Runtime,
            ProgramPublisherParameters.RedundantFileNames, ProgramPublisherParameters.UploadTempExtension,
            ProgramPublisherParameters.FileStorageForExchange, ProgramPublisherParameters.SmartSchemaForLocal,
            ProgramPublisherParameters.SmartSchemaForExchange);
        return createPackageAndUpload.Run();
    }
}