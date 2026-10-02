using System.Collections.Generic;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsData.Models;

namespace LibSupportToolsServerWork.Registry;

//ველები, რომლებიც არასოდეს სინქრონიზდება (README §4.1, G6, G9): ისინი ამ კომპიუტერს ეკუთვნის ან ყოველ კომპიუტერზე
//თავიდან ითვლება. სერვერზე არ მიდის და სერვერიდან ჩამოტანა მათ არ ცვლის. C3/C4-ის mapper-ები და ჰეშის ნორმალიზაცია
//ამ სიას ეყრდნობა. ახალი კომპიუტერის ველი აქაც უნდა დაემატოს (README §4.6).
//ჩანაწერის დონეზეც ერთი გამონაკლისია: ApiClients-ის ის ჩანაწერი, რომელსაც SupportToolsServerWebApiClientName
//ასახელებს, ლოკალური რჩება (bootstrap, G6)
public static class MachineLocalFields
{
    //SupportToolsParameters-ის ზედა დონის ველები. აქ დაემატება RegistrySyncState (C2) და ავტოსინქრონიზაციის ალამი (D2)
    public static IReadOnlyList<string> TopLevel { get; } =
    [
        nameof(SupportToolsParameters.MachineName),
        nameof(SupportToolsParameters.CurrentMachineServerName),
        nameof(SupportToolsParameters.PathMappings),
        nameof(SupportToolsParameters.SupportToolsServerWebApiClientName),
        nameof(SupportToolsParameters.LogFolder),
        nameof(SupportToolsParameters.LogGitWork),
        nameof(SupportToolsParameters.GitExecutablePath),
        nameof(SupportToolsParameters.WorkFolder),
        nameof(SupportToolsParameters.FolderForGitignoreFiles),
        nameof(SupportToolsParameters.FolderForEditorConfigFiles),
        nameof(SupportToolsParameters.TempFolder),
        nameof(SupportToolsParameters.CodeGenerateTestFolder),
        nameof(SupportToolsParameters.SecurityFolder),
        nameof(SupportToolsParameters.ScaffoldSeedersWorkFolder),
        nameof(SupportToolsParameters.PublisherWorkFolder),
        nameof(SupportToolsParameters.LocalInstallerSettings),
        nameof(SupportToolsParameters.RecentCommandsFileName),
        nameof(SupportToolsParameters.RecentCommandsCount),
        //გამოთვლადია და ყოველ კომპიუტერზე თავიდან ითვლება (G9)
        nameof(SupportToolsParameters.GitProjects),
        //ამ კომპიუტერის exe ფაილების გზებია
        nameof(SupportToolsParameters.Archivers)
    ];

    //IsLocal = (სერვერის სახელი == CurrentMachineServerName), იხ. ServersIsLocalCalculator
    public static IReadOnlyList<string> Server { get; } = [nameof(ServerDataModel.IsLocal)];

    //დაყენებული და ბოლო ვერსია და გამოსაძახებელი ბრძანება ამ კომპიუტერისაა
    public static IReadOnlyList<string> DotnetTool { get; } =
    [
        nameof(DotnetToolData.InstalledVersion),
        nameof(DotnetToolData.LatestVersion),
        nameof(DotnetToolData.CommandName)
    ];

    //DatabasesBackupFilesExchangeParameters-ის ლოკალური ფოლდერი
    public static IReadOnlyList<string> DatabasesBackupFilesExchange { get; } =
        [nameof(DatabasesBackupFilesExchangeParameters.LocalPath)];
}
