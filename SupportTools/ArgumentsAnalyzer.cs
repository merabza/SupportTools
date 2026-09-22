using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.Threading.Tasks;
using SupportToolsData;
using SystemTools.SystemToolsShared;

namespace SupportTools;

//პროგრამისთვის გადმოცემული ბრძანებათა სტრიქონის არგუმენტების გაანალიზება
public sealed class ArgumentsAnalyzer
{
    private readonly Option<string?> _projectOption;
    private readonly RootCommand _rootCommand;
    private readonly Option<string?> _runOption;
    private readonly Option<string?> _serverOption;
    private readonly Option<string?> _useOption;
    private ParseResult? _parseResult;

    public ArgumentsAnalyzer()
    {
        _useOption = new Option<string?>("--use", "-u")
        {
            Description = "File name for use as parameters json."
        };

        _projectOption = new Option<string?>("--project", "-p")
        {
            Description = "Project name, for which the tool must be run. Required together with --run."
        };

        _serverOption = new Option<string?>("--server", "-s")
        {
            Description =
                "Server name or ServerName|EnvironmentName. Required together with --run, when a server tool is specified."
        };

        _runOption = new Option<string?>("--run", "-r")
        {
            Description =
                "Run this project or server tool for the project specified by --project and exit, without showing the menu."
        };

        _runOption.CompletionSources.Add(ProjectToolRunner.GetAllToolNames());

        _rootCommand = new RootCommand("SupportTools - support tools for .NET projects.")
        {
            _useOption, _projectOption, _serverOption, _runOption
        };
    }

    public string? ParametersFileName { get; private set; }
    public string? ProjectName { get; private set; }
    public string? ServerName { get; private set; }
    public EProjectTools? ProjectTool { get; private set; }
    public EProjectServerTools? ServerTool { get; private set; }
    public int ExitCode { get; private set; }

    //აბრუნებს true-ს, თუ პროგრამის მუშაობა უნდა გაგრძელდეს.
    //false-ის შემთხვევაში პროგრამა ExitCode-ით უნდა დასრულდეს
    public async ValueTask<bool> Analysis(string[] args)
    {
        ParseResult parseResult = _rootCommand.Parse(args);
        _parseResult = parseResult;

        //პარსინგის შეცდომები, --help და --version თვითონ System.CommandLine-მა დაამუშაოს
        if (parseResult.Errors.Count > 0 || parseResult.Action is not null)
        {
            ExitCode = await parseResult.InvokeAsync();
            return false;
        }

        string? parametersFileName = parseResult.GetValue(_useOption);
        ParametersFileName = string.IsNullOrWhiteSpace(parametersFileName) ? null : parametersFileName;
        ProjectName = parseResult.GetValue(_projectOption);
        ServerName = parseResult.GetValue(_serverOption);

        string? runToolName = parseResult.GetValue(_runOption);

        if (!string.IsNullOrWhiteSpace(runToolName))
        {
            return AnalyzeToolName(runToolName);
        }

        WarnAboutUnusedOptions();
        return true;

    }

    //პარამეტრების გამოყენების ინსტრუქციის გამოტანა
    public void ShowHelp()
    {
        if (_parseResult is null)
        {
            return;
        }

        var helpAction = new HelpAction();
        helpAction.Invoke(_parseResult);
    }

    private bool AnalyzeToolName(string runToolName)
    {
        ProjectTool = ProjectToolRunner.FindProjectTool(runToolName);
        if (ProjectTool is null)
        {
            ServerTool = ProjectToolRunner.FindServerTool(runToolName);
        }

        if (ProjectTool is null && ServerTool is null)
        {
            return Error($"Tool with name {runToolName} does not exists");
        }

        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            return Error("--project (-p) must be specified together with --run (-r)");
        }

        if (ServerTool is not null && string.IsNullOrWhiteSpace(ServerName))
        {
            return Error($"--server (-s) must be specified for server tool {ServerTool}");
        }

        if (ProjectTool is not null && !string.IsNullOrWhiteSpace(ServerName))
        {
            StShared.WriteWarningLine($"--server (-s) is not used for tool {ProjectTool}, it will be ignored", true);
        }

        return true;
    }

    private void WarnAboutUnusedOptions()
    {
        if (!string.IsNullOrWhiteSpace(ProjectName))
        {
            StShared.WriteWarningLine("--project (-p) works only together with --run (-r), it will be ignored", true);
        }

        if (!string.IsNullOrWhiteSpace(ServerName))
        {
            StShared.WriteWarningLine("--server (-s) works only together with --run (-r), it will be ignored", true);
        }
    }

    private bool Error(string errorText)
    {
        StShared.WriteErrorLine(errorText, true, null, false);
        Console.WriteLine();
        ShowHelp();
        ExitCode = 5;
        return false;
    }
}
