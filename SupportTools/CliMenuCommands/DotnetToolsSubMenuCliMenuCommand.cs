//using System;
//using System.Linq;
//using CliMenu;
//using CliParameters.CliMenuCommands;
//using LibDataInput;
//using LibParameters;
//using SupportTools.DotnetTools;

//namespace SupportTools.CliMenuCommands;

//public sealed class DotnetToolsSubMenuCliMenuCommand : CliMenuCommand
//{
//    private readonly IParametersManager _parametersManager;

//    // ReSharper disable once ConvertToPrimaryConstructor
//    public DotnetToolsSubMenuCliMenuCommand(IParametersManager parametersManager) : base("Dotnet Tools",
//        EMenuAction.LoadSubMenu)
//    {
//        _parametersManager = parametersManager;
//    }

//    public override CliMenuSet GetSubMenu()
//    {
//        CliMenuSet dotnetToolsSubMenuSet = new("Dotnet Tools");

//        //ქვემენიუს ელემენტები

//        //ყველას საჭირო ინსტრუმენტის განახლებაზე შემოწმება
//        //var dotnetToolsSearched = CreateListOfDotnetToolsInstalled();

//        //ყველას საჭირო ინსტრუმენტის განახლება ან დაყენება
//        //dotnet tool update --global dotnet-ef
//        //dotnet tool install --global dotnet-ef
//        //var dotnetAllToolsCreateOrUpdateCommand = new DotnetAllToolsCreateOrUpdateCommand();

//        //ყველა ინსტრუმენტის განახლება ბოლო ვერსიამდე
//        var updateAllToolsToLatestVersionCommand = new UpdateAllToolsToLatestVersionCliMenuCommand(_parametersManager);
//        dotnetToolsSubMenuSet.AddMenuItem(updateAllToolsToLatestVersionCommand);

//        //დაინსტალირებული ინსტრუმენტების სიის დადგენა
//        var dotnetTools = DotnetToolsManager.Instance;
//        if (dotnetTools is not null)
//        {
//            var dotnetToolsInstalled = dotnetTools.DotnetTools;

//            //დაინსტალირებული ინსტრუმენტების სიის გამოტანა მენიუში
//            foreach (var tool in dotnetToolsInstalled.OrderBy(x => x.PackageId))
//                dotnetToolsSubMenuSet.AddMenuItem(new DotnetToolSubMenuCliMenuCommand(tool));
//        }

//        //var dotnetToolCommand = new DotnetToolCliMenuCommand();
//        //dotnetToolsSubMenuSet.AddMenuItem(dotnetToolCommand);

//        //მთავარ მენიუში გასვლა
//        var key = ConsoleKey.Escape.Value().ToLower();
//        dotnetToolsSubMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to level up menu", null),
//            key.Length);

//        return dotnetToolsSubMenuSet;
//    }
//}


