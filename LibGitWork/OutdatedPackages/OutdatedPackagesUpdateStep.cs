namespace LibGitWork.OutdatedPackages;

//git, რომლის პაკეტებიც პროექტის ფოლდერში ახლდება
public sealed record OutdatedPackagesUpdateStep(string ProjectName, string GitName);
