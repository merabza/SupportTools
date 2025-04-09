//Created by ProjectMainClassCreator at 12/22/2020 19:46:17

using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class ServicePublisherToolCommand : ToolCommand
{
    private const string ActionName = "Publishing Service";
    private const string ActionDescription = "Publishing Service";
    private readonly AppSettingsEncoderParameters _appSettingsEncoderParameters;
    private readonly ILogger _logger;

    public ServicePublisherToolCommand(ILogger logger, ProgramPublisherParameters parameters,
        AppSettingsEncoderParameters appSettingsEncoderParametersForPublish, IParametersManager parametersManager) :
        base(logger, ActionName, parameters, parametersManager, ActionDescription)
    {
        _logger = logger;
        _appSettingsEncoderParameters = appSettingsEncoderParametersForPublish;
    }

    private ProgramPublisherParameters ProgramPublisherParameters => (ProgramPublisherParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //1. შევქმნათ საინსტალაციო პაკეტი და ავტვირთოთ ფაილსაცავში
        var createPackageAndUpload = new CreatePackageAndUpload(_logger, ProgramPublisherParameters.ProjectName,
            ProgramPublisherParameters.MainProjectFileName, ProgramPublisherParameters.ServerInfo,
            ProgramPublisherParameters.WorkFolder, ProgramPublisherParameters.DateMask,
            ProgramPublisherParameters.Runtime, ProgramPublisherParameters.RedundantFileNames,
            ProgramPublisherParameters.UploadTempExtension, ProgramPublisherParameters.FileStorageForExchange,
            ProgramPublisherParameters.SmartSchemaForLocal, ProgramPublisherParameters.SmartSchemaForExchange);
        if (!await createPackageAndUpload.Run(cancellationToken))
            return false;

        //2. დავშიფროთ პარამეტრების ფაილი და ავტვირთოთ ფაილსაცავში
        //AppSettingsEncoderParameters appSettingsEncoderParameters =
        //    ProgramPublisherParameters.AppSettingsEncoderParameters;
        var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(_logger,
            _appSettingsEncoderParameters.AppSetEnKeysJsonFileName,
            _appSettingsEncoderParameters.AppSettingsJsonSourceFileName,
            _appSettingsEncoderParameters.AppSettingsEncodedJsonFileName, _appSettingsEncoderParameters.KeyPart1,
            _appSettingsEncoderParameters.KeyPart2, ProgramPublisherParameters.ProjectName,
            ProgramPublisherParameters.ServerInfo, ProgramPublisherParameters.DateMask,
            _appSettingsEncoderParameters.ParametersFileExtension, ProgramPublisherParameters.FileStorageForExchange,
            ProgramPublisherParameters.SmartSchemaForExchange);
        return await encodeParametersAndUploadAction.Run(cancellationToken);
    }
}