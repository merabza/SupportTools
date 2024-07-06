using System;
using System.IO;
using SystemToolsShared;

namespace SupportTools.Models;

public sealed class ClassCreatorInfo
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ClassCreatorInfo(string sourceFileFullPath, string destinationFolder, string className,
        string destinationCodeFileName)
    {
        SourceFileFullPath = sourceFileFullPath;
        DestinationFolder = destinationFolder;
        ClassName = className;
        DestinationCodeFileName = destinationCodeFileName;
    }

    public string SourceFileFullPath { get; set; }
    public string DestinationFolder { get; set; }
    public string ClassName { get; set; }
    public string DestinationCodeFileName { get; set; }

    public static ClassCreatorInfo? Create(string path, bool creatorCreatesClass)
    {
        var file = new FileInfo(path);
        if (file.DirectoryName is null)
        {
            Console.WriteLine("file.DirectoryName is null");
            StShared.Pause();
            return null;
        }

        var className = Path.GetFileNameWithoutExtension(file.Name).Replace(".", string.Empty).Capitalize() +
                        (creatorCreatesClass ? "Class" : string.Empty) + "Creator";
        var codeFileName = className + ".cs";
        return new ClassCreatorInfo(path, Path.Combine(file.DirectoryName, "CodeCreators"), className, codeFileName);
    }
}