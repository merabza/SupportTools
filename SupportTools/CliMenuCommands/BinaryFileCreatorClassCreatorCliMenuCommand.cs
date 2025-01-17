using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CliMenu;
using LibAppProjectCreator.CodeCreators;
using LibDataInput;
using LibMenuInput;
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

    protected override bool RunBody()
    {
        var path = MenuInputer.InputFileOrFolderPath("File or folder path with binary files", null);
        if (path is null)
            return false;

        if (File.Exists(path))
        {
            Console.WriteLine("Entered path {0} is a file", path);

            var file = new FileInfo(path);

            if (file.DirectoryName is null)
            {
                Console.WriteLine("file.DirectoryName is null");
                return false;
            }

            var classCreatorInfo = ClassCreatorInfo.Create(path, false);
            if (classCreatorInfo is null)
                return false;

            ProcessFiles([classCreatorInfo]);

            return true;
        }

        if (Directory.Exists(path))
        {
            Console.WriteLine("Entered path {0} is a Folder", path);

            var infos = new List<ClassCreatorInfo>();
            var dir = new DirectoryInfo(path);
            foreach (var file in dir.GetFiles())
            {
                var classCreatorInfo = ClassCreatorInfo.Create(file.FullName, false);
                if (classCreatorInfo is null)
                    return false;
                infos.Add(classCreatorInfo);
            }

            ProcessFiles(infos);
            return true;
        }


        Console.WriteLine("File or folder with name {0} is not exists", path);
        return false;
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
        foreach (var cci in classCreatorInfos)
        {
            Console.WriteLine($"Loading {cci.SourceFileFullPath}...");
            var fileBytes = File.ReadAllBytes(cci.SourceFileFullPath);
            var base64String = Convert.ToBase64String(fileBytes);

            Console.WriteLine($"Creating {cci.DestinationCodeFileName}...");
            var creatorClassCreator = new BinFileCreatorClassCreator(_logger, cci.DestinationFolder, cci.ClassName,
                base64String, cci.DestinationCodeFileName);
            creatorClassCreator.CreateFileStructure();
        }
    }

    private static bool CheckDestinationFilesExists(List<ClassCreatorInfo> classCreatorInfos)
    {
        var existsFileNames = classCreatorInfos
            .Select(classCreatorInfo => new
            {
                classCreatorInfo,
                toGenerateFileName =
                    Path.Combine(classCreatorInfo.DestinationFolder, classCreatorInfo.DestinationCodeFileName)
            }).Where(x => File.Exists(x.toGenerateFileName))
            .Select(sx => sx.classCreatorInfo.DestinationCodeFileName).ToList();

        if (existsFileNames.Count <= 0)
            return false;

        Console.WriteLine("Destination Files already exists: ");
        foreach (var fileName in existsFileNames) Console.WriteLine($"    {fileName}");

        return true;
    }
}