namespace LibGitWork.OutdatedPackages;

//git, რომლის პაკეტებიც პროექტის ფოლდერში ახლდება
internal sealed record OutdatedPackagesUpdateStep(string ProjectName, string GitName);
