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
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class BinaryFileCreatorClassCreatorCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public BinaryFileCreatorClassCreatorCliMenuCommand(ILogger logger) : base(
        "Create binary file Class Creator")
    {
        _logger = logger;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;

        try
        {
            //SupportToolsParameters parameters = (SupportToolsParameters)_parametersManager.Parameters;

            var path = MenuInputer.InputFileOrFolderPath("File or folder path with binary files", null);
            if (path is null)
                return;

            if (File.Exists(path))
            {
                Console.WriteLine("Entered path {0} is a file", path);

                var file = new FileInfo(path);

                if (file.DirectoryName is null)
                {
                    Console.WriteLine("file.DirectoryName is null");
                    StShared.Pause();
                    return;
                }

                var classCreatorInfo = ClassCreatorInfo.Create(path, false);
                if (classCreatorInfo is null)
                    return;

                ProcessFiles([classCreatorInfo]);

                StShared.Pause();
                return;
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
                        return;
                    infos.Add(classCreatorInfo);
                }

                ProcessFiles(infos);


                StShared.Pause();
                return;
            }


            Console.WriteLine("File or folder with name {0} does not exists", path);
            StShared.Pause();

            return;
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

        MenuAction = EMenuAction.Reload;
    }


    private void ProcessFiles(List<ClassCreatorInfo> classCreatorInfos)
    {
        if (CheckDestinationFilesExists(classCreatorInfos))
            if (!Inputer.InputBool("Overwrite?", false))
                return;

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
            var creatorClassCreator = new BinFileCreatorClassCreator(_logger, cci.DestinationFolder,
                cci.ClassName, base64String, cci.DestinationCodeFileName);
            creatorClassCreator.CreateFileStructure();
        }
    }

    private static bool CheckDestinationFilesExists(List<ClassCreatorInfo> classCreatorInfos)
    {
        var existsFileNames = (classCreatorInfos
            .Select(classCreatorInfo => new
            {
                classCreatorInfo,
                toGenerateFileName =
                    Path.Combine(classCreatorInfo.DestinationFolder, classCreatorInfo.DestinationCodeFileName)
            })
            .Where(x => File.Exists(x.toGenerateFileName))
            .Select(sx => sx.classCreatorInfo.DestinationCodeFileName)).ToList();

        if (existsFileNames.Count <= 0)
            return false;

        Console.WriteLine("Destination Files already exists: ");
        foreach (var fileName in existsFileNames) Console.WriteLine(@$"    {fileName}");

        return true;
    }
}