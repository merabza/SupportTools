﻿using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.GitIgnoreCreators;

public sealed class ReactProjectGitIgnoreCreator : CodeCreator
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ReactProjectGitIgnoreCreator(ILogger logger, string placePath, string? codeFileName = null) : base(logger,
        placePath, codeFileName)
    {
    }

    public override void CreateFileStructure()
    {
        var text = new TextCode(@"# These are some examples of commonly ignored file patterns.
# You should customize this list as applicable to your project.
# Learn more about .gitignore:
#     https://www.atlassian.com/git/tutorials/saving-changes/gitignore

# Node artifact files
node_modules/
dist/

# Compiled Java sealed class files
*.sealed class

# Compiled Python bytecode
*.py[cod]

# Log files
*.log

# Package files
*.jar

# Maven
target/
dist/

# JetBrains IDE
.idea/

# Unit test reports
TEST*.xml

# Generated by MacOS
.DS_Store

# Generated by Windows
Thumbs.db

# Applications
*.app
*.exe
*.war

# Large media files
*.mp4
*.tiff
*.avi
*.flv
*.mov
*.wmv

");
        CodeFile.Add(text);
        FinishAndSave();
    }
}