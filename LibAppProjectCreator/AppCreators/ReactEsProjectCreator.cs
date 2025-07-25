﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;
using HtmlAgilityPack;
using LibNpmWork;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.AppCreators;

public sealed class ReactEsProjectCreator
{
    private const string SdkRef = "https://www.nuget.org/packages/Microsoft.VisualStudio.JavaScript.SDK";
    private readonly string _createInPath;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ILogger _logger;
    private readonly string _projectFileName;
    private readonly string _projectFolderName;
    private readonly string _projectName;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReactEsProjectCreator(ILogger logger, IHttpClientFactory httpClientFactory, string createInPath,
        string projectFolderName, string projectFileName, string projectName, bool useConsole)
    {
        _logger = logger;
        _projectFileName = projectFileName;
        _projectName = projectName;
        _useConsole = useConsole;
        _httpClientFactory = httpClientFactory;
        _createInPath = createInPath;
        _projectFolderName = projectFolderName;
    }

    public bool Create()
    {
        //შეიქმნას ფოლდერი სადაც უნდა ჩაიწეროს რეაქტის ფრონტ პროექტი
        StShared.CreateFolder(_createInPath, _useConsole);

        var npmProcessor = new NpmProcessor(_logger);

        if (!npmProcessor.CreatingReactAppUsingVite(_createInPath, _projectName))
            return false;

        /*
           cd {_projectName}
           npm install
           npm run dev
         */

        var sdkUri = new Uri(SdkRef);
        var (statusCode, content) = GetOnePageContent(sdkUri);

        if (statusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(content))
            return false;

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(content);

        var refsTitlesList = ExtractAllLinks(htmlDoc.DocumentNode);
        const string startString = "/packages/";
        var res = refsTitlesList.Where(x => x.Item1.StartsWith(startString))
            .OrderByDescending(x => x.Item2, new VersionComparer());

        var result = res.FirstOrDefault().Item1;
        var javaScriptSdk = result[startString.Length..];
        CreateEsprojFile(Path.Combine(_createInPath, _projectFolderName, _projectFileName), javaScriptSdk);

        //Microsoft.VisualStudio.JavaScript.Sdk-ს ბოლო ვერსიის დასადგენად უნდა მოვქაჩოთ გვერდი 
        //https://www.nuget.org/packages/Microsoft.VisualStudio.JavaScript.SDK
        //გავაანალიზოთ მოქაჩული html დოკუმენტი.
        //ამოვიღოთ ყველა ასეთი ლინკი
        //<a href="/packages/Microsoft.VisualStudio.JavaScript.SDK/1.0.1477582" title="1.0.1477582">
        //პირველი, ან ყველაზე მაღალ ნომრიანი გამოვიყენოთ .esproj ფაილის xml-ის შესაქმნელად
        //დანარჩენი ინფორმაცია ამ xml-ში გადავა ისე, როგორც ნიმუშშია.

        //შეიქმნას რეაქტის პროექტი შესაბამისი სკრიპტის გამოყენებით 
        //npx create-react-app my-app --template typescript

        //შევქმნათ src ფოლდერი და მისი შიგთავსი

        return true;
    }

    private static List<(string, string)> ExtractAllLinks(HtmlNode htmlDocDocumentNode)
    {
        var links = htmlDocDocumentNode.SelectNodes("//a[@href]");
        if (links is { Count: 0 })
            return [];

        return (from link in links
            let hrefValue = link.GetAttributeValue("href", string.Empty)
            let titleValue = link.GetAttributeValue("title", string.Empty)
            where !string.IsNullOrWhiteSpace(hrefValue) && !string.IsNullOrWhiteSpace(titleValue) &&
                  char.IsDigit(titleValue[0])
            select (hrefValue, titleValue)).ToList();
    }

    private (HttpStatusCode, string?) GetOnePageContent(Uri uri)
    {
        try
        {
            // ReSharper disable once using
            var client = _httpClientFactory.CreateClient();
            // ReSharper disable once using
            using var response = client.GetAsync(uri).Result;

            return response.IsSuccessStatusCode
                ? (response.StatusCode, response.Content.ReadAsStringAsync().Result)
                : (response.StatusCode, null);
        }
        catch
        {
            StShared.WriteErrorLine($"Error when downloading {uri}", true, _logger, false);
            //StShared.WriteException(e, true);
        }

        return (HttpStatusCode.BadRequest, null);
    }

    private static void CreateEsprojFile(string projectFileFullName, string javaScriptSdk)
    {
        /*
        <Project Sdk="Microsoft.VisualStudio.JavaScript.Sdk/1.0.2752196">
             <PropertyGroup>
               <StartupCommand>npm run dev</StartupCommand>
               <JavaScriptTestRoot>src\</JavaScriptTestRoot>
               <JavaScriptTestFramework>Vitest</JavaScriptTestFramework>
               <!-- Allows the build (or compile) script located on package.json to run on Build -->
               <ShouldRunBuildScript>false</ShouldRunBuildScript>
               <!-- Folder where production build objects will be placed -->
               <BuildOutputFolder>$(MSBuildProjectDirectory)\dist</BuildOutputFolder>
             </PropertyGroup>
           </Project>
        */
        var project = new XElement("Project", new XAttribute("Sdk", javaScriptSdk),
            new XElement("PropertyGroup", new XElement("StartupCommand", "npm run dev"),
                new XElement("JavaScriptTestRoot", "src\\"), new XElement("JavaScriptTestFramework", "Vitest"),
                new XComment(" Allows the build (or compile) script located on package.json to run on Build "),
                new XElement("ShouldRunBuildScript", false),
                new XComment(" Folder where production build objects will be placed "),
                new XElement("BuildOutputFolder", "$(MSBuildProjectDirectory)\\dist")));
        project.Save(projectFileFullName);
    }

    //private static void CreateEsprojFileOld1(string projectFileFullName, string javaScriptSdk)
    //{
    /*
        <Project Sdk="Microsoft.VisualStudio.JavaScript.Sdk/0.5.45-alpha">
            <PropertyGroup>
        <StartupCommand>set BROWSER=none&amp;&amp;npm start</StartupCommand>
        <JavaScriptTestRoot>src\</JavaScriptTestRoot>
        <JavaScriptTestFramework>Jest</JavaScriptTestFramework>
        <!-- Command to run on project build -->
        <BuildCommand></BuildCommand>
        <!-- Command to create an optimized build of the project that's ready for publishing -->
        <ProductionBuildCommand>npm run build</ProductionBuildCommand>
        <!-- Folder where production build objects will be placed -->
        <BuildOutputFolder>$(MSBuildProjectDirectory)\build</BuildOutputFolder>
        </PropertyGroup>
        </Project>
    */
    //    var project = new XElement("Project", new XAttribute("Sdk", javaScriptSdk),
    //        new XElement("PropertyGroup", new XElement("StartupCommand", "set BROWSER=none&amp;&amp;npm start"),
    //            new XElement("JavaScriptTestRoot", "src\\"), new XElement("JavaScriptTestFramework", "Jest"),
    //            new XComment(" Command to run on project build "), new XElement("BuildCommand"),
    //            new XComment(" Command to create an optimized build of the project that's ready for publishing "),
    //            new XElement("ProductionBuildCommand", "npm run build"),
    //            new XComment(" Folder where production build objects will be placed "),
    //            new XElement("BuildOutputFolder", "$(MSBuildProjectDirectory)\\build")));
    //    project.Save(projectFileFullName);
    //}
}