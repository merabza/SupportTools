namespace LibGitWork.SyncV3;

//სინქრონიზაციის ერთი ფოლდერი: რომელი git-ის (Gits-ის გასაღები, ის SupportToolsServer-ის სახელიცაა) და რომელი პროექტის
internal sealed record GitSyncFolder(string GitName, string GitAddress, string ProjectName, string FolderPath);
