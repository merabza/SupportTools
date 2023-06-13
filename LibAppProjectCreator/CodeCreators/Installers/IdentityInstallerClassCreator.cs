////Created by CreatorClassCreator at 7/2/2022 8:50:49 PM

//using System;
//using CodeTools;
//using Microsoft.Extensions.Logging;

//namespace LibAppProjectCreator.CodeCreators.Installers;

//public sealed class IdentityInstallerClassCreator : CodeCreator
//{
//    private readonly string _projectNamespace;
//    private readonly bool _useServerCarcass;

//    public IdentityInstallerClassCreator(ILogger logger, string placePath, string projectNamespace,
//        bool useServerCarcass, string? codeFileName = null) :
//        base(logger, placePath, codeFileName)
//    {
//        _projectNamespace = projectNamespace;
//        _useServerCarcass = useServerCarcass;
//    }

//    public override void CreateFileStructure()
//    {
//        var block = new CodeBlock("",
//            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
//            "using System",
//            "using System.Text",
//            _useServerCarcass ? "using CarcassIdentity" : null,
//            "using Microsoft.AspNetCore.Authentication.JwtBearer",
//            "using Microsoft.AspNetCore.Builder",
//            _useServerCarcass ? "using Microsoft.AspNetCore.Identity" : null,
//            "using Microsoft.Extensions.Configuration",
//            "using Microsoft.Extensions.DependencyInjection",
//            "using Microsoft.IdentityModel.Tokens",
//            _useServerCarcass ? "using ServerCarcass.Models" : $"using {_projectNamespace}.Models",
//            "",
//            $"namespace {_projectNamespace}.Installers",
//            "",
//            new OneLineComment(" ReSharper disable once UnusedType.Global"),
//            new CodeBlock("public sealed class IdentityInstaller : IInstaller, IAppMiddlewareInstaller",
//                "public int InstallPriority => 30",
//                "public int MiddlewarePriority => 30",
//                new CodeBlock("public void InstallServices(WebApplicationBuilder builder, string[] args)",
//                    "Console.WriteLine(\"identityInstaller.InstallServices Started\")",
//                    "",
//                    _useServerCarcass
//                        ? new FlatCodeBlock("builder.Services.AddScoped<IUserStore<AppUser>, MyUserStore>()",
//                            "builder.Services.AddScoped<IUserPasswordStore<AppUser>, MyUserStore>()",
//                            "builder.Services.AddScoped<IUserEmailStore<AppUser>, MyUserStore>()",
//                            "builder.Services.AddScoped<IUserRoleStore<AppUser>, MyUserStore>()",
//                            "builder.Services.AddScoped<IRoleStore<AppRole>, MyUserStore>()",
//                            "",
//                            @"builder.Services.AddIdentity<AppUser, AppRole>(options =>
//        { 
//            options.Password.RequiredLength = 3;
//            options.Password.RequireNonAlphanumeric = false;
//            options.Password.RequireLowercase = false;
//            options.Password.RequireUppercase = false;
//            options.Password.RequireDigit = false;
//        }).AddDefaultTokenProviders()",
//                            "")
//                        : null,
//                    new OneLineComment(" configure strongly typed settings objects"),
//                    "var appSettingsSection = builder.Configuration.GetSection(\"AppSettings\")",
//                    "builder.Services.Configure<AppSettings>(appSettingsSection)",
//                    "",
//                    new OneLineComment(" configure jwt authentication"),
//                    "var appSettings = appSettingsSection.Get<AppSettings>()",
//                    "string? jwtSecret = appSettings.JwtSecret",
//                    new CodeBlock("if (jwtSecret is null)",
//                        "throw new Exception(\"jwtSecret is null\")"),
//                    "byte[] key = Encoding.ASCII.GetBytes(jwtSecret)",
//                    @"builder.Services.AddAuthentication(x =>
//        {
//            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//            x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
//        }).AddJwtBearer(x =>
//        {
//            x.RequireHttpsMetadata = false;
//            x.SaveToken = true;
//            x.TokenValidationParameters = new TokenValidationParameters
//            {
//                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(key),
//                ValidateIssuer = false, ValidateAudience = false
//            };
//        })",
//                    "",
//                    "Console.WriteLine(\"identityInstaller.InstallServices Finished\")"),
//                "",
//                new CodeBlock("public void UseMiddleware(WebApplication app)",
//                    "app.UseAuthentication()",
//                    "app.UseAuthorization()")));
//        CodeFile.AddRange(block.CodeItems);
//        FinishAndSave();

//    }

//}

