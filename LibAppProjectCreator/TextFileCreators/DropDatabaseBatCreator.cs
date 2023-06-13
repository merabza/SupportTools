//using CodeTools.CodeCreators;
//using Microsoft.Extensions.Logging;

//namespace LibAppProjectCreator.TextFileCreators;

//public sealed class DropDatabaseBatCreator : TextFileCreator
//{

//    private readonly string _projectNamespace;

//    public DropDatabaseBatCreator(ILogger logger, string placePath, string projectNamespace,
//        string textFileName = null) : base(logger, placePath, textFileName)
//    {
//        _projectNamespace = projectNamespace;
//    }


//    public override void Create()
//    {
//        TextBuilder.AppendLine(
//            $"dotnet ef database drop --force --context {_projectNamespace}DbContext --startup-project {_projectNamespace}\\{_projectNamespace}.csproj --project {_projectNamespace}DbMigration\\{_projectNamespace}DbMigration.csproj");
//        TextBuilder.AppendLine("rem Remove-Item $migrationCsFiles");
//        TextBuilder.AppendLine("");
//        TextBuilder.AppendLine("pause");
//        FinishAndSave();
//    }

//}

