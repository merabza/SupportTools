using System;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibCodeGenerator.Helpers;
using LibCodeGenerator.Models;
using Microsoft.Extensions.Logging;

namespace LibCodeGenerator.ToolCommands;

public sealed class GenerateApiRoutesToolCommand : ToolCommand
{
    private readonly ILogger _logger;
    private readonly GenerateApiRoutesToolParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GenerateApiRoutesToolCommand(ILogger logger, GenerateApiRoutesToolParameters parameters) : base(logger,
        "Data Seeder", parameters, null, "Seeds data from existing Json files")
    {
        _logger = logger;
        _parameters = parameters;
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //// Check if there are changes in the current project
        //var hasChanges = await GitHelper.HasUncommittedChangesAsync(_parameters.ProjectPath);
        //if (hasChanges)
        //{
        //    _logger.LogWarning("There are uncommitted changes in the project.");
        //    Console.WriteLine("There are uncommitted changes in the project. Continue? (y/n)");
        //    var input = Console.ReadLine();
        //    if (!string.Equals(input, "y", StringComparison.OrdinalIgnoreCase))
        //        return false;
        //}

        //// Update project files in the code generation test folder using git
        //bool updateSuccess = await GitHelper.UpdateTestFolderAsync(_parameters.CodeGenerateTestFolder, cancellationToken);
        //if (!updateSuccess)
        //{
        //    _logger.LogError("Failed to update test folder.");
        //    return false;
        //}

        //// Find the ApiRoutes class file
        //string? apiRoutesFilePath = ApiRoutesFinder.FindApiRoutesFile(_parameters.ProjectPath);
        //if (apiRoutesFilePath is null)
        //{
        //    _logger.LogError("ApiRoutes class file not found.");
        //    return false;
        //}

        //// Generate ApiRoutes class code with all required routes
        //string generatedCode = ApiRoutesGenerator.GenerateApiRoutesClass(_parameters);

        //// Write generated code to the corresponding file
        //await File.WriteAllTextAsync(apiRoutesFilePath, generatedCode, cancellationToken);

        //// Show git diff for the changed code
        //string diff = await GitHelper.GetFileDiffAsync(apiRoutesFilePath);
        //Console.WriteLine("Changes in ApiRoutes class:");
        //Console.WriteLine(diff);

        //// Show success message
        //Console.WriteLine("ApiRoutes code successfully generated.");

        return true;
    }
}