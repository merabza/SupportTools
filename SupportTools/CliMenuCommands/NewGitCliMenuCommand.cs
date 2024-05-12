using System;
using System.Linq;
using CliMenu;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class NewGitCliMenuCommand : CliMenuCommand
{
    private readonly EGitCol _gitCol;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    //ახალი პროექტის შექმნის ამოცანა
    // ReSharper disable once ConvertToPrimaryConstructor
    public NewGitCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName,
        EGitCol gitCol) : base(
        "Add Git Project")
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitCol = gitCol;
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

            var gitProjectNames = _gitCol switch
            {
                EGitCol.Main => project.GitProjectNames,
                EGitCol.ScaffoldSeed => project.ScaffoldSeederGitProjectNames,
                _ => throw new ArgumentOutOfRangeException()
            };

            //გადავამოწმოთ ხომ არ არსებობს იგივე სახელით სხვა პროექტი.
            if (gitProjectNames.Any(a => a == newGitName))
            {
                StShared.WriteErrorLine(
                    $"Git Project with Name {newGitName} in project {_projectName} is already exists. cannot create Server with this name. ",
                    true, _logger);
                return;
            }

            //switch (_gitCol)
            //{
            //    //პროექტის დამატება პროექტების სიაში
            //    case EGitCol.Main:
            //        project.GitProjectNames.Add(newGitName);
            //        break;
            //    case EGitCol.ScaffoldSeed:
            //        project.ScaffoldSeederGitProjectNames.Add(newGitName);
            //        break;
            //    default:
            //        throw new ArgumentOutOfRangeException();
            //}
            gitProjectNames.Add(newGitName);

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
            //StShared.Pause();
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
        }
    }
}