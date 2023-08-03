﻿using System;
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
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class CreatorClassCreatorCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;

    public CreatorClassCreatorCliMenuCommand(ILogger logger) : base(
        "Create Class Creator")
    {
        _logger = logger;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;

        try
        {
            //SupportToolsParameters parameters = (SupportToolsParameters)_parametersManager.Parameters;

            var path = MenuInputer.InputFileOrFolderPath("File or folder path with cs code", null);
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

                var classCreatorInfo = ClassCreatorInfo.Create(path, true);
                if (classCreatorInfo is null)
                    return;

                ProcessFiles(new List<ClassCreatorInfo> { classCreatorInfo });

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
                    var classCreatorInfo = ClassCreatorInfo.Create(file.FullName, true);
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
            Console.WriteLine($"Analize {cci.SourceFileFullPath}...");
            var fileContent = File.ReadAllText(cci.SourceFileFullPath);
            var lines = fileContent.Split(Environment.NewLine);
            var lineData = lines.Select(LineData.Create).ToList();
            for (var i = 1; i < lineData.Count - 1; i++)
                if (lineData[i].DryLine == "")
                    lineData[i].Indent = lineData[i - 1].Indent;
            lineData.Add(LineData.CreateEndOfFile());


            var stackItems = new Stack<CodeBlockBase>();
            var startCodeBlock =
                new CodeBlock("", "new OneLineComment($\"Created by {GetType().Name} at {DateTime.Now}\")");
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
                else if (lineData[i].DryLine == "")
                {
                    currentCodeBlock.Add(new CodeExtraLine());
                }
                else if (lineData[i].TrimLine.EndsWith(";"))
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
            var creatorClassCreator = new CreatorClassCreator(_logger, cci.DestinationFolder,
                cci.ClassName, startCodeBlock, cci.DestinationCodeFileName);
            creatorClassCreator.CreateFileStructure();
        }
    }

    private bool CheckDestinationFilesExists(List<ClassCreatorInfo> classCreatorInfos)
    {
        var existsFileNames = new List<string>();
        foreach (var classCreatorInfo in classCreatorInfos)
        {
            var toGenerateFileName =
                Path.Combine(classCreatorInfo.DestinationFolder, classCreatorInfo.DestinationCodeFileName);
            if (File.Exists(toGenerateFileName))
                existsFileNames.Add(classCreatorInfo.DestinationCodeFileName);
        }

        if (existsFileNames.Count <= 0)
            return false;

        Console.WriteLine("Destination Files already exists: ");
        foreach (var fileName in existsFileNames) Console.WriteLine(@$"    {fileName}");

        return true;
    }
}