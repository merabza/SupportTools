using System;
using System.Linq;
using CliMenu;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class NewGitCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    //ახალი პროექტის შექმნის ამოცანა
    public NewGitCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName) : base(
        "Add Git Project")
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override void RunAction()
    {
        Console.WriteLine("Add new Git started");
        try
        {
            GitCruder gitCruder = new(_logger, _parametersManager);
            var newGitName = gitCruder.GetNameWithPossibleNewName("Git Name", null);

            if (string.IsNullOrWhiteSpace(newGitName))
            {
                StShared.WriteErrorLine($"project with name {_projectName} does not exists", true);
                return;
            }

            //მიმდინარე პარამეტრები
            var parameters = (SupportToolsParameters)_parametersManager.Parameters;
            var project = parameters.GetProject(_projectName);

            if (project is null)
            {
                StShared.WriteErrorLine($"project with name {_projectName} does not exists", true);
                return;
            }

            //გადავამოწმოთ ხომ არ არსებობს იგივე სახელით სხვა პროექტი.
            if (project.GitProjectNames.Any(a => a == newGitName))
            {
                StShared.WriteErrorLine(
                    $"Git Project with Name {newGitName} in project {_projectName} is already exists. cannot create Server with this name. ",
                    true, _logger);
                return;
            }

            //პროექტის დამატება პროექტების სიაში
            project.GitProjectNames.Add(newGitName);

            //ცვლილებების შენახვა
            _parametersManager.Save(parameters, "Add Git Project Finished");

            //მოვითხოვოთ მენიუს ახლიდან ჩატვირთვა. ჩაიტვირთოს მთავარი მენიუ
            //MenuState = new MenuState {RebuildMenu = true};
            MenuAction = EMenuAction.Reload;

            //პაუზა იმისათვის, რომ პროცესის მიმდინარეობის შესახებ წაკითხვა მოვასწროთ და მივხვდეთ, რომ პროცესი დასრულდა
            //StShared.Pause();

            //ყველაფერი კარგად დასრულდა
        }
        catch (DataInputEscapeException)
        {
            Console.WriteLine();
            Console.WriteLine("Escape... ");
            StShared.Pause();
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
        }
    }
}