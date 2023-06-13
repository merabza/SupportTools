//using System.IO;
//using LibAppProjectCreator.Models;
//using Microsoft.Extensions.Logging;
//using SystemToolsShared;

//namespace LibScaffoldSeeder.Models;

//public sealed class ScaffoldSeederAppCreatorData
//{
//    public AppCreatorBaseData AppCreatorBaseData { get; }
//    //public string ReactClientPath { get; }
//    //public string MainDatabaseProjectName { get; }

//    public static ScaffoldSeederAppCreatorData? CreateScaffoldSeederAppCreatorData(ILogger logger, AppCreatorBaseData appCreatorBaseData,
//        AppProjectCreatorData par, ScaffoldSeederCreatorParameters scaffoldSeederCreatorParameters)
//    {

//        //if (par.TempFolderPath is null)
//        //{
//        //    StShared.WriteErrorLine($"Temp folder does not specified", true, logger);
//        //    return null;
//        //}

//        ////დროებით ფოლდერი არ უნდა ემთხვეოდეს სამუშაო ფოლდერს
//        //if (FileStat.NormalizePath(par.WorkFolderPath) == FileStat.NormalizePath(par.TempFolderPath))
//        //{
//        //    StShared.WriteErrorLine($"Work and Temp Folders are same {par.TempFolderPath}.", true, logger);
//        //    return null;
//        //}

//        ////დროებით ფოლდერი არ უნდა ემთხვეოდეს საიდუმლოებების ფოლდერს
//        //if (FileStat.NormalizePath(par.SecurityWorkFolderPath) == FileStat.NormalizePath(par.TempFolderPath))
//        //{
//        //    StShared.WriteErrorLine($"Security and Temp Folders are same {par.TempFolderPath}.", true, logger);
//        //    return null;
//        //}

//        ////დროებით ფოლდერი არ უნდა ემთხვეოდეს პროექტის სამუშაო ფოლდერს
//        //if (FileStat.NormalizePath(appCreatorBaseData.WorkPath) == FileStat.NormalizePath(par.TempFolderPath))
//        //{
//        //    StShared.WriteErrorLine($"project Work and Temp Folders are same {par.TempFolderPath}.", true,
//        //        logger);
//        //    return null;
//        //}

//        ////დროებით ფოლდერი არ უნდა ემთხვეოდეს პროექტის საიდუმლოებების ფოლდერს
//        //if (FileStat.NormalizePath(appCreatorBaseData.SecurityPath) == FileStat.NormalizePath(par.TempFolderPath))
//        //{
//        //    StShared.WriteErrorLine($"project Work and Temp Folders are same {par.TempFolderPath}.", true,
//        //        logger);
//        //    return null;
//        //}

//        ////შევამოწმოთ და თუ არ არსებობს შევქმნათ დროებითი სამუშაოებისათვის განკუთვნილი ფოლდერი
//        //if (!StShared.CreateFolder(par.TempFolderPath, true))
//        //{
//        //    StShared.WriteErrorLine($"Cannot create temp Folder {par.TempFolderPath}", true, logger);
//        //    return null;
//        //}

//        ////შევამოწმოთ არსებობს თუ არა უკვე პროექტის ფოლდერი
//        //string tempPath = Path.Combine(par.TempFolderPath, par.ProjectName);
//        ////foldersForCreate.Add(tempPath);
//        //AppCreatorBaseData.CheckRequiredFolder(tempPath, false);

//        //დავიანგარიშოთ კლიენტის ფოლდერის სრული გზა
//        //string reactClientPath = Path.Combine(appCreatorBaseData.WorkPath, $"{par.ProjectName}.client");

//        return new ScaffoldSeederAppCreatorData(appCreatorBaseData);
//    }


//    private ScaffoldSeederAppCreatorData(AppCreatorBaseData appCreatorBaseData)
//    {
//        AppCreatorBaseData = appCreatorBaseData;
//    }
//}

