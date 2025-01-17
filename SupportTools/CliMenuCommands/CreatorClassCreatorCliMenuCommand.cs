using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CliMenu;
using CodeTools;
using LibAppProjectCreator.CodeCreators;
using LibDataInput;
using LibMenuInput;
using Microsoft.Extensions.Logging;
using SupportTools.Models;

namespace SupportTools.CliMenuCommands;

public sealed class CreatorClassCreatorCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CreatorClassCreatorCliMenuCommand(ILogger logger) : base("Create Class Creator", EMenuAction.Reload)
    {
        _logger = logger;
    }

    protected override bool RunBody()
    {
        var path = MenuInputer.InputFileOrFolderPath("File or folder path with cs code", null);
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

            var classCreatorInfo = ClassCreatorInfo.Create(path, true);
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
                var classCreatorInfo = ClassCreatorInfo.Create(file.FullName, true);
                if (classCreatorInfo is null)
                    return false;
                infos.Add(classCreatorInfo);
            }

            ProcessFiles(infos);

            return true;
        }


        Console.WriteLine("File or folder with name {0} does not exists", path);
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
            Console.WriteLine($"Analyze {cci.SourceFileFullPath}...");
            var fileContent = File.ReadAllText(cci.SourceFileFullPath);
            var lines = fileContent.Split(Environment.NewLine);
            var lineData = lines.Select(LineData.Create).ToList();
            for (var i = 1; i < lineData.Count - 1; i++)
                if (lineData[i].DryLine == string.Empty)
                    lineData[i].Indent = lineData[i - 1].Indent;
            lineData.Add(LineData.CreateEndOfFile());


            var stackItems = new Stack<CodeBlockBase>();
            var startCodeBlock = new CodeBlock(string.Empty,
                "new OneLineComment($\"Created by {GetType().Name} at {DateTime.Now}\")");
            CodeBlockBase mainCodeBlock = startCodeBlock;
            var currentCodeBlock = mainCodeBlock;
            stackItems.Push(currentCodeBlock);
            var currentIndent = 0;
            for (var i = 0; i < lineData.Count - 1; i++)
            {
                if (lineData[i].TrimLine.StartsWith("//"))
                {
                    currentCodeBlock.Add(new OneLineComment(lineData[i].TrimLine[2..]));
                }
                else if (lineData[i].DryLine == string.Empty)
                {
                    currentCodeBlock.Add(new CodeExtraLine());
                }
                else if (lineData[i].TrimLine.EndsWith(';'))
                {
                    currentCodeBlock.Add(new CodeCommand(lineData[i].TrimLine[..^1]));
                }
                else if (lineData[i + 1].TrimLine == "{" || lineData[i + 1].Indent > currentIndent)
                {
                    var newBlock = new CodeBlock(lineData[i].TrimLine);
                    currentCodeBlock.Add(newBlock);
                    stackItems.Push(currentCodeBlock);
                    currentCodeBlock = newBlock;
                    if (lineData[i + 1].TrimLine == "{")
                        i++;
                    currentIndent = lineData[i + 1].Indent;
                }

                if (lineData[i + 1].TrimLine == "}" || lineData[i + 1].Indent < currentIndent)
                {
                    while (lineData[i + 1].Indent < currentIndent)
                    {
                        currentCodeBlock = stackItems.Pop();
                        currentIndent--;
                    }

                    if (lineData[i + 1].TrimLine == "}")
                        i++;
                }
            }

            Console.WriteLine($"Creating {cci.DestinationCodeFileName}...");
            var creatorClassCreator = new CreatorClassCreator(_logger, cci.DestinationFolder, cci.ClassName,
                startCodeBlock, cci.DestinationCodeFileName);
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