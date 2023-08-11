//Created by ProjectMainClassCreator at 12/22/2020 19:46:17

using CliParameters;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ServicePublisher : ToolCommand
{
    private const string ActionName = "Publishing Service";
    private const string ActionDescription = "Publishing Service";
    private readonly AppSettingsEncoderParameters _appSettingsEncoderParameters;

    public ServicePublisher(ILogger logger, bool useConsole, ProgramPublisherParameters parameters,
        AppSettingsEncoderParameters appSettingsEncoderParametersForPublish, IParametersManager parametersManager) :
        base(logger, useConsole, ActionName, parameters, parametersManager, ActionDescription)
    {
        _appSettingsEncoderParameters = appSettingsEncoderParametersForPublish;
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
        if (!createPackageAndUpload.Run())
            return false;

        //2. დავშიფროთ პარამეტრების ფაილი და ავტვირთოთ ფაილსაცავში
        //AppSettingsEncoderParameters appSettingsEncoderParameters =
        //    ProgramPublisherParameters.AppSettingsEncoderParameters;
        var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(Logger, UseConsole,
            _appSettingsEncoderParameters.AppSetEnKeysJsonFileName,
            _appSettingsEncoderParameters.AppSettingsJsonSourceFileName,
            _appSettingsEncoderParameters.AppSettingsEncodedJsonFileName, _appSettingsEncoderParameters.KeyPart1,
            _appSettingsEncoderParameters.KeyPart2, ProgramPublisherParameters.ProjectName,
            ProgramPublisherParameters.ServerInfo, ProgramPublisherParameters.DateMask,
            _appSettingsEncoderParameters.ParametersFileExtension, ProgramPublisherParameters.FileStorageForExchange,
            ProgramPublisherParameters.SmartSchemaForExchange);
        return encodeParametersAndUploadAction.Run();
    }
}