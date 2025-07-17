//Created by ProjectMainClassCreator at 12/22/2020 19:46:17

using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ProgramPublisherToolCommand : ToolCommand
{
    private const string ActionName = "Publishing App";
    private const string ActionDescription = "Publishing App";
    private readonly ILogger _logger;
    private readonly ProgramPublisherParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProgramPublisherToolCommand(ILogger logger, ProgramPublisherParameters parameters,
        IParametersManager parametersManager) : base(logger, ActionName, parameters, parametersManager,
        ActionDescription)
    {
        _logger = logger;
        _parameters = parameters;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //1. შევქმნათ საინსტალაციო პაკეტი და ავტვირთოთ ფაილსაცავში
        var createPackageAndUpload = new CreatePackageAndUpload(_logger, _parameters.ProjectName,
            _parameters.MainProjectFileName, _parameters.ServerInfo, _parameters.WorkFolder, _parameters.DateMask,
            _parameters.Runtime, _parameters.RedundantFileNames, _parameters.UploadTempExtension,
            _parameters.FileStorageForExchange, _parameters.SmartSchemaForLocal, _parameters.SmartSchemaForExchange);
        return await createPackageAndUpload.Run(cancellationToken);
    }
}