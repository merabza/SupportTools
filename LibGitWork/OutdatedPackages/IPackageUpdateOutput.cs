using System;

namespace LibGitWork.OutdatedPackages;

//ერთი git-ის განახლების გამოტანა: მიმდინარე ნაბიჯი (დაფის სტრიქონი) და სტრიქონები, რომლებიც დაფის ზემოთ რჩება
internal interface IPackageUpdateOutput
{
    void SetStatus(string status);

    void WriteLine(string text, ConsoleColor? color);
}
