using System;

namespace LibGitWork.OutdatedPackages;

//ერთი git-ის განახლება პარალელურ განახლებაში. WorkspaceFolder - პროექტის ფოლდერი, რომელშიც git და მისი მეზობელი
//რეპოები (../X) არის: ერთი ფოლდერის სამუშაოები ერთდროულად არ ეშვება
internal sealed record PackageUpdateJob(
    string GitName,
    string WorkspaceFolder,
    Func<IPackageUpdateOutput, EPackageUpdateResult> Run);
