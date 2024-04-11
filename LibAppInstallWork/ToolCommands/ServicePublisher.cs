//Created by ProjectMainClassCreator at 12/22/2020 19:46:17

using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class ServicePublisher : ToolCommand
{
    private const string ActionName = "Publishing Service";
    private const string ActionDescription = "Publishing Service";
    private readonly AppSettingsEncoderParameters _appSettingsEncoderParameters;

    public ServicePublisher(ILogger logger, ProgramPublisherParameters parameters,
        AppSettingsEncoderParameters appSettingsEncoderParametersForPublish, IParametersManager parametersManager) :
        base(logger, ActionName, parameters, parametersManager, ActionDescription)
    {
        _appSettingsEncoderParameters = appSettingsEncoderParametersForPublish;
    }

    private ProgramPublisherParameters ProgramPublisherParameters => (ProgramPublisherParameters)Par;

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        //1. შევქმნათ საინსტალაციო პაკეტი და ავტვირთოთ ფაილსაცავში
        var createPackageAndUpload = new CreatePackageAndUpload(Logger, ProgramPublisherParameters.ProjectName,
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
        var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(Logger,
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