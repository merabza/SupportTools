////Created by CreatorClassCreator at 7/2/2022 8:50:49 PM

//using System;
//using CodeTools;
//using Microsoft.Extensions.Logging;

//namespace LibAppProjectCreator.CodeCreators.Installers;

//public sealed class KestrelServerOptionsInstallerClassCreator : CodeCreator
//{
//    private readonly string _projectNamespace;

//    public KestrelServerOptionsInstallerClassCreator(ILogger logger, string placePath, string projectNamespace,
//        string? codeFileName = null) : base(logger, placePath, codeFileName)
//    {
//        _projectNamespace = projectNamespace;
//    }


//    public override void CreateFileStructure()
//    {
//        var block = new CodeBlock("",
//            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
//            "using System",
//            "using Microsoft.AspNetCore.Builder",
//            "using Microsoft.AspNetCore.Server.Kestrel.Core",
//            "using Microsoft.Extensions.DependencyInjection",
//            "",
//            $"namespace {_projectNamespace}.Installers",
//            "",
//            new OneLineComment(" ReSharper disable once UnusedType.Global"),
//            new CodeBlock("public sealed class KestrelServerOptionsInstaller : IInstaller",
//                "public int InstallPriority => 30",
//                "",
//                new CodeBlock("public void InstallServices(WebApplicationBuilder builder, string[] args)",
//                    "Console.WriteLine(\"KestrelServerOptionsInstaller.InstallServices Started\")",
//                    "",
//                    "builder.Services.Configure<KestrelServerOptions>(options => { options.AllowSynchronousIO = true; })",
//                    "",
//                    "",
//                    "Console.WriteLine(\"KestrelServerOptionsInstaller.InstallServices Finished\")"),
//                ""));
//        CodeFile.AddRange(block.CodeItems);
//        FinishAndSave();

//    }
//}

