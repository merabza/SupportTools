//using System.Linq;
//using System.Xml.Linq;

//namespace LibAppProjectCreator.ProjectFileXmlModifiers;

//public sealed class ProjectFileXmlModifierForReact
//{
//    private readonly XElement _projectXml;

//    public ProjectFileXmlModifierForReact(XElement projectXml)
//    {
//        _projectXml = projectXml;
//    }

//    public bool Run()
//    {
//        //var grpXml = CheckAddProjectGroup(_projectXml, "PropertyGroup");

//        //AddProjectParametersWithCheck(grpXml, "TypeScriptCompileBlocked", "true");
//        //AddProjectParametersWithCheck(grpXml, "TypeScriptToolsVersion", "Latest");
//        //AddProjectParametersWithCheck(grpXml, "IsPackable", "false");
//        //AddProjectParametersWithCheck(grpXml, "SpaRoot", "ClientApp\\");
//        //AddProjectParametersWithCheck(grpXml, "DefaultItemExcludes","$(DefaultItemExcludes);$(SpaRoot)node_modules\\**");

//        //AddSpaRootItemGroup(_projectXml);
//        //AddNodeTarget(_projectXml);
//        //AddPublishTarget(_projectXml);

//        return true;
//    }

//    //private void AddProjectParametersWithCheck(XElement projectXml, string groupName, string propertyName, string propertyValue)
//    //{
//    //    var grpXml = CheckAddProjectGroup(projectXml, groupName);
//    //    AddProjectParametersWithCheck(grpXml, propertyName, propertyValue);
//    //}

//    private XElement CheckAddProjectGroup(XElement projectXml, string groupName)
//    {
//        var grpXml = projectXml.Descendants(groupName).SingleOrDefault();
//        if (grpXml is null)
//        {
//            grpXml = new XElement(groupName);
//            projectXml.Add(grpXml);
//        }

//        return grpXml;
//    }

//    //private void AddProjectParametersWithCheck(XElement grpXml, string propertyName, string propertyValue)
//    //{
//    //    var propXml = grpXml.Descendants(propertyName).SingleOrDefault();
//    //    if (propXml is null)
//    //    {
//    //        propXml = new XElement(propertyName, propertyValue);
//    //        grpXml.Add(propXml);
//    //    }
//    //    else
//    //    {
//    //        propXml.Value = propertyValue;
//    //    }
//    //}

//    //private void AddSpaRootItemGroup(XElement projectXml)
//    //{
//    //    projectXml.Add(new XElement("ItemGroup",
//    //        new XComment("Don't publish the SPA source files, but do show them in the project files list"),
//    //        new XElement("Content", new XAttribute("Remove", "$(SpaRoot)**")),
//    //        new XElement("None", new XAttribute("Remove", "$(SpaRoot)**")),
//    //        new XElement("None", new XAttribute("Include", "$(SpaRoot)**"),
//    //            new XAttribute("Exclude", "$(SpaRoot)node_modules\\**"))));
//    //}

//    //private void AddNodeTarget(XElement projectXml)
//    //{
//    //    projectXml.Add(new XElement("Target", new XAttribute("Name", "DebugEnsureNodeEnv"),
//    //        new XAttribute("BeforeTargets", "Build"),
//    //        new XAttribute("Condition", " '$(Configuration)' == 'Debug' And !Exists('$(SpaRoot)node_modules') "),
//    //        new XComment("Ensure Node.js is installed"),
//    //        new XElement("Exec", new XAttribute("Command", "node --version"), new XAttribute("ContinueOnError", "true"),
//    //            new XElement("Output", new XAttribute("TaskParameter", "ExitCode"),
//    //                new XAttribute("PropertyName", "ErrorCode"))),
//    //        new XElement("Error", new XAttribute("Condition", "'$(ErrorCode)' != '0'"),
//    //            new XAttribute("Text",
//    //                "Node.js is required to build and run this project. To continue, please install Node.js from https://nodejs.org/, and then restart your command prompt or IDE.")),
//    //        new XElement("Message", new XAttribute("Importance", "high"),
//    //            new XAttribute("Text", "Restoring dependencies using 'npm'. This may take several minutes...")),
//    //        new XElement("Exec", new XAttribute("WorkingDirectory", "$(SpaRoot)"),
//    //            new XAttribute("Command", "npm install"))));
//    //}

//    //private void AddPublishTarget(XElement projectXml)
//    //{
//    //    projectXml.Add(new XElement("Target", new XAttribute("Name", "PublishRunWebpack"),
//    //        new XAttribute("AfterTargets", "ComputeFilesToPublish"),
//    //        new XComment("As part of publishing, ensure the JS resources are freshly built in production mode"),
//    //        new XElement("Exec", new XAttribute("WorkingDirectory", "$(SpaRoot)"),
//    //            new XAttribute("Command", "npm install")),
//    //        new XElement("Exec", new XAttribute("WorkingDirectory", "$(SpaRoot)"),
//    //            new XAttribute("Command", "npm run build")),
//    //        new XComment("Include the newly-built files in the publish output"),
//    //        new XElement("ItemGroup",
//    //            new XElement("DistFiles", new XAttribute("Include", "$(SpaRoot)build\\**; $(SpaRoot)build-ssr\\**")),
//    //            new XElement("ResolvedFileToPublish", new XAttribute("Include", "@(DistFiles->'%(FullPath)')"),
//    //                new XAttribute("Exclude", "@(ResolvedFileToPublish)"),
//    //                new XElement("RelativePath", "%(DistFiles.Identity)"),
//    //                new XElement("CopyToPublishDirectory", "PreserveNewest"),
//    //                new XElement("ExcludeFromSingleFile", "true")))));
//    //}
//}