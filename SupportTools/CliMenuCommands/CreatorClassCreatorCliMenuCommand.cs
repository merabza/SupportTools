using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CodeTools;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using LibAppProjectCreator.CodeCreators;
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

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        string? path = MenuInputer.InputFileOrFolderPath("File or folder path with cs code", null);
        if (path is null)
        {
            return ValueTask.FromResult(false);
        }

        if (File.Exists(path))
        {
            Console.WriteLine("Entered path {0} is a file", path);

            var file = new FileInfo(path);

            if (file.DirectoryName is null)
            {
                Console.WriteLine("file.DirectoryName is null");
                return ValueTask.FromResult(false);
            }

            var classCreatorInfo = ClassCreatorInfo.Create(path, true);
            if (classCreatorInfo is null)
            {
                return ValueTask.FromResult(false);
            }

            ProcessFiles([classCreatorInfo]);

            return ValueTask.FromResult(true);
        }

        if (Directory.Exists(path))
        {
            Console.WriteLine("Entered path {0} is a Folder", path);

            var infos = new List<ClassCreatorInfo>();
            var dir = new DirectoryInfo(path);
            foreach (FileInfo file in dir.GetFiles())
            {
                var classCreatorInfo = ClassCreatorInfo.Create(file.FullName, true);
                if (classCreatorInfo is null)
                {
                    return ValueTask.FromResult(false);
                }

                infos.Add(classCreatorInfo);
            }

            ProcessFiles(infos);

            return ValueTask.FromResult(true);
        }

        Console.WriteLine("File or folder with name {0} does not exists", path);
        return ValueTask.FromResult(false);
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
            Console.WriteLine($"Analyze {cci.SourceFileFullPath}...");
            string fileContent = File.ReadAllText(cci.SourceFileFullPath);
            string[] lines = fileContent.Split(Environment.NewLine);
            List<LineData> lineData = lines.Select(LineData.Create).ToList();
            for (int i = 1; i < lineData.Count - 1; i++)
            {
                if (string.IsNullOrEmpty(lineData[i].DryLine))
                {
                    lineData[i].Indent = lineData[i - 1].Indent;
                }
            }

            lineData.Add(LineData.CreateEndOfFile());

            var stackItems = new Stack<CodeBlockBase>();
            var startCodeBlock = new CodeBlock(string.Empty,
                "new OneLineComment($\"Created by {GetType().Name} at {DateTime.Now}\")");
            CodeBlockBase mainCodeBlock = startCodeBlock;
            CodeBlockBase currentCodeBlock = mainCodeBlock;
            stackItems.Push(currentCodeBlock);
            int currentIndent = 0;
            int wi = 0;
            while (wi < lineData.Count - 1)
            {
                if (lineData[wi].TrimLine.StartsWith("//", StringComparison.InvariantCulture))
                {
                    currentCodeBlock.Add(new OneLineComment(lineData[wi].TrimLine[2..]));
                }
                else if (string.IsNullOrEmpty(lineData[wi].DryLine))
                {
                    currentCodeBlock.Add(new CodeExtraLine());
                }
                else if (lineData[wi].TrimLine.EndsWith(';'))
                {
                    currentCodeBlock.Add(new CodeCommand(lineData[wi].TrimLine[..^1]));
                }
                else if (lineData[wi + 1].TrimLine == "{" || lineData[wi + 1].Indent > currentIndent)
                {
                    var newBlock = new CodeBlock(lineData[wi].TrimLine);
                    currentCodeBlock.Add(newBlock);
                    stackItems.Push(currentCodeBlock);
                    currentCodeBlock = newBlock;
                    if (lineData[wi + 1].TrimLine == "{")
                    {
                        wi++;
                    }

                    currentIndent = lineData[wi + 1].Indent;
                }

                if (lineData[wi + 1].TrimLine == "}" || lineData[wi + 1].Indent < currentIndent)
                {
                    while (lineData[wi + 1].Indent < currentIndent)
                    {
                        currentCodeBlock = stackItems.Pop();
                        currentIndent--;
                    }

                    if (lineData[wi + 1].TrimLine == "}")
                    {
                        wi++;
                    }
                }

                wi++;
            }

            Console.WriteLine($"Creating {cci.DestinationCodeFileName}...");
            var creatorClassCreator = new CreatorClassCreator(_logger, cci.DestinationFolder, cci.ClassName,
                startCodeBlock, cci.DestinationCodeFileName);
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
