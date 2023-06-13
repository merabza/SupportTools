//using CodeTools.CodeCreators;
//using Microsoft.Extensions.Logging;

//namespace LibAppProjectCreator.TextFileCreators;

//public sealed class DatabaseMigrationBatCreator : TextFileCreator
//{

//    private readonly string _projectNamespace;

//    public DatabaseMigrationBatCreator(ILogger logger, string placePath, string projectNamespace,
//        string textFileName = null) : base(logger, placePath, textFileName)
//    {
//        _projectNamespace = projectNamespace;
//    }

//    public override void Create()
//    {
//        TextBuilder.AppendLine($"rem --context {_projectNamespace}DbContext");
//        TextBuilder.AppendLine("rem start with \"Initial\"");
//        TextBuilder.AppendLine(
//            $"dotnet ef migrations add \"Initial\" --context {_projectNamespace}DbContext --startup-project {_projectNamespace}\\{_projectNamespace}.csproj --project {_projectNamespace}DbMigration\\{_projectNamespace}DbMigration.csproj");
//        TextBuilder.AppendLine($"dotnet ef database update --context {_projectNamespace}DbContext --startup-project {_projectNamespace}\\{_projectNamespace}.csproj --project {_projectNamespace}DbMigration\\{_projectNamespace}DbMigration.csproj");
//        TextBuilder.AppendLine("pause");
//        FinishAndSave();
//    }

//}

