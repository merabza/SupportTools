using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using LibAppProjectCreator.CodeCreators;
using Microsoft.Extensions.Logging;
using SupportTools.Models;

namespace SupportTools.CliMenuCommands;

public sealed class BinaryFileCreatorClassCreatorCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public BinaryFileCreatorClassCreatorCliMenuCommand(ILogger logger) : base("Create binary file Class Creator",
        EMenuAction.Reload)
    {
        _logger = logger;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        string? path = MenuInputer.InputFileOrFolderPath("File or folder path with binary files", null);
        if (path is null)
        {
            return new ValueTask<bool>(false);
        }

        if (File.Exists(path))
        {
            Console.WriteLine("Entered path {0} is a file", path);

            var file = new FileInfo(path);

            if (file.DirectoryName is null)
            {
                Console.WriteLine("file.DirectoryName is null");
                return new ValueTask<bool>(false);
            }

            var classCreatorInfo = ClassCreatorInfo.Create(path, false);
            if (classCreatorInfo is null)
            {
                return new ValueTask<bool>(false);
            }

            ProcessFiles([classCreatorInfo]);

            return new ValueTask<bool>(true);
        }

        if (Directory.Exists(path))
        {
            Console.WriteLine("Entered path {0} is a Folder", path);

            var infos = new List<ClassCreatorInfo>();
            var dir = new DirectoryInfo(path);
            foreach (FileInfo file in dir.GetFiles())
            {
                var classCreatorInfo = ClassCreatorInfo.Create(file.FullName, false);
                if (classCreatorInfo is null)
                {
                    return new ValueTask<bool>(false);
                }

                infos.Add(classCreatorInfo);
            }

            ProcessFiles(infos);
            return new ValueTask<bool>(true);
        }

        Console.WriteLine("File or folder with name {0} is not exists", path);
        return new ValueTask<bool>(false);
    }

    private void ProcessFiles(List<ClassCreatorInfo> classCreatorInfos)
    {
        if (CheckDestinationFilesExists(classCreatorInfos) && !Inputer.InputBool("Overwrite?", false))
        {
            MenuAction = EMenuAction.Reload;
            return;
        }

        CreateCodeFiles(classCreatorInfos);
    }

    private void CreateCodeFiles(List<ClassCreatorInfo> classCreatorInfos)
    {
        foreach (ClassCreatorInfo cci in classCreatorInfos)
        {
            Console.WriteLine($"Loading {cci.SourceFileFullPath}...");
            byte[] fileBytes = File.ReadAllBytes(cci.SourceFileFullPath);
            string base64String = Convert.ToBase64String(fileBytes);

            Console.WriteLine($"Creating {cci.DestinationCodeFileName}...");
            var creatorClassCreator = new BinFileCreatorClassCreator(_logger, cci.DestinationFolder, cci.ClassName,
                base64String, cci.DestinationCodeFileName);
            creatorClassCreator.CreateFileStructure();
        }
    }

    private static bool CheckDestinationFilesExists(List<ClassCreatorInfo> classCreatorInfos)
    {
        List<string> existsFileNames = classCreatorInfos
            .Select(classCreatorInfo => new
            {
                classCreatorInfo,
                toGenerateFileName =
                    Path.Combine(classCreatorInfo.DestinationFolder, classCreatorInfo.DestinationCodeFileName)
            }).Where(x => File.Exists(x.toGenerateFileName))
            .Select(sx => sx.classCreatorInfo.DestinationCodeFileName).ToList();

        if (existsFileNames.Count <= 0)
        {
            return false;
        }

        Console.WriteLine("Destination Files already exists: ");
        foreach (string fileName in existsFileNames)
        {
            Console.WriteLine($"    {fileName}");
        }

        return true;
    }
}
