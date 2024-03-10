using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.Database;

public sealed class DesignTimeDbContextFactoryClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    public DesignTimeDbContextFactoryClassCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "",
            "using System",
            "using System.IO",
            "using Microsoft.EntityFrameworkCore",
            "using Microsoft.EntityFrameworkCore.Design",
            "using Microsoft.Extensions.Configuration",
            "",
            $"namespace {_projectNamespace}Db",
            "",
            new OneLineComment("ეს არის ზოგადი კლასი მიგრაციასთან სამუშაოდ"),
            new OneLineComment("თუმცა მე გადავაკეთე ისე, რომ კონსტრუქტორი ღებულობს ინფორმაციას:"),
            new OneLineComment(
                "1. სად ეძებოს პარამეტრების ფაილი, ==\\== ამის გადაცემა გადავიფიქრე, რადგან კოდი მიმდინარე ფოლდერში იყურება"),
            new OneLineComment(
                "და უფრო სწორია, თუ მიგრაციის პროცესს იმ ფოლდერიდან გავუშვებთ, რომელშიც პარამეტრებია ჩაწერილი."),
            new OneLineComment(
                "მიგრაციის და ბაზის კონტექსტის შესახებ ინფორმაციის გადაწოდება კი მიგრაციის ბრძანებისათვის შესაძლებელია და სკრიპტიდან მოხდება"),
            new OneLineComment("2. რა ჰქვია პარამეტრების ფაილს"),
            new OneLineComment("3. რომელ პარამეტრში წერია ბაზასთან დასაკავშირებელი სტრიქონი"),
            new OneLineComment(
                "ბაზასთან დაკავშირების სტრიქონის გადმოწოდება არასწორია, რადგან მომიწევდა ამ სტრიქონის გამშვები პროექტის კოდში ჩაშენება."),
            new OneLineComment("რაც უსაფრთხოების თვალსაზრისით არასწორია"),
            "",
            new CodeBlock(
                "public class DesignTimeDbContextFactory<T> : IDesignTimeDbContextFactory<T> where T : DbContext",
                "",
                "private readonly string _assemblyName",
                "private readonly string _connectionParamName",
                "private readonly string? _parametersJsonFileName",
                "",
                new CodeBlock(
                    "protected DesignTimeDbContextFactory(string assemblyName, string connectionParamName, string? parametersJsonFileName = null)",
                    "_assemblyName = assemblyName",
                    "_connectionParamName = connectionParamName",
                    "_parametersJsonFileName = parametersJsonFileName",
                    "Console.WriteLine($\"DesignTimeDbContextFactory assemblyName = {assemblyName}\")",
                    "Console.WriteLine($\"DesignTimeDbContextFactory connectionParamName = {connectionParamName}\")",
                    "Console.WriteLine($\"DesignTimeDbContextFactory parametersJsonFileName = {parametersJsonFileName}\")"
                ),
                new CodeBlock("public T CreateDbContext(string[] args)",
                    new OneLineComment(
                        "თუ პარამეტრების json ფაილის სახელი პირდაპირ არ არის გადმოცემული, ვიყენებთ სტანდარტულ სახელს appsettings.json"),
                    "var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(_parametersJsonFileName ?? \"appsettings.json\", false, true).Build()",
                    new OneLineComment(
                        ".AddEncryptedJsonFile(Path.Combine(pathToContentRoot, \"appsettingsEncoded.json\"), optional: false, reloadOnChange: true, Key,"),
                    new OneLineComment("  Path.Combine(pathToContentRoot, \"appsetenkeys.json\"))"),
                    new OneLineComment(".AddUserSecrets<TSt>()"),
                    new OneLineComment(".AddEnvironmentVariables()"),
                    "var connectionString = configuration[_connectionParamName]",
                    "Console.WriteLine($\"DesignTimeDbContextFactory CreateDbContext connectionString = {connectionString}\")",
                    "",
                    "var builder = new DbContextOptionsBuilder<T>()",
                    "builder.UseSqlServer(connectionString, b => b.MigrationsAssembly(_assemblyName))",
                    "var dbContext = Activator.CreateInstance(typeof(T), builder.Options, true)",
                    "return dbContext is null ? throw new Exception(\"dbContext does not created\") : (T)dbContext"
                )
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}