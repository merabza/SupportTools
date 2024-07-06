////Created by CreatorClassCreator at 7/2/2022 8:50:49 PM

//using System;
//using CodeTools;
//using Microsoft.Extensions.Logging;

//namespace LibAppProjectCreator.CodeCreators.BackgroundTasks;

//public sealed class ProjectBackgroundTasksQueueInstallerClassCreator : CodeCreator
//{
//    private readonly string _projectNamespace;
//    private readonly bool _useServerCarcass;

//    public ProjectBackgroundTasksQueueInstallerClassCreator(ILogger logger, string placePath, string projectNamespace,
//        bool useServerCarcass, string? codeFileName = null) : base(logger, placePath, codeFileName)
//    {
//        _projectNamespace = projectNamespace;
//        _useServerCarcass = useServerCarcass;
//    }

//    public override void CreateFileStructure()
//    {
//        var block = new CodeBlock(string.Empty,
//            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
//            "using System",
//            "using Microsoft.AspNetCore.Builder",
//            "using Microsoft.Extensions.DependencyInjection",
//            _useServerCarcass ? "using ServerCarcass.FilterSort" : null,
//            string.Empty,
//            $"namespace {_projectNamespace}.Installers",
//            string.Empty,
//            new OneLineComment(" ReSharper disable once UnusedType.Global"),
//            new CodeBlock("public sealed class ProjectBackgroundTasksQueueInstaller : IInstaller",
//                "public int InstallPriority => 30",
//                string.Empty,
//                new CodeBlock("public void InstallServices(WebApplicationBuilder builder, string[] args)",
//                    "Console.WriteLine(\"ModelPartBackgroundTasksQueueInstaller.InstallServices Started\")",
//                    string.Empty,
//                    new OneLineComment("builder.Services.AddHostedService<ModelPartQueuedHostedService>()"),
//                    new OneLineComment(
//                        "builder.Services.AddSingleton<IModelPartBackgroundTaskQueue, ModelPartBackgroundTaskQueue>()"),
//                    "builder.Services.AddSignalR()",
//                    string.Empty,
//                    "Console.WriteLine(\"ModelPartBackgroundTasksQueueInstaller.InstallServices Finished\")"),
//                string.Empty));
//        CodeFile.AddRange(block.CodeItems);
//        FinishAndSave();
//    }
//}

