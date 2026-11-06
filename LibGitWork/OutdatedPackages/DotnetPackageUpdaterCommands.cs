using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using SystemTools.SharedKernel;

namespace LibGitWork.OutdatedPackages;

//Update Outdated Packages-ის dotnet ბრძანებები. გამოტანა კონსოლზე არ გადის (git-ები პარალელურად ახლდება და ის დაფას
//დაარღვევდა): stdout და stderr მოსვლის რიგით ერთად ინახება და შეცდომის ტექსტში ბრუნდება, რადგან restore-ისა და
//dotnet outdated-ის მიზეზი ხშირად stdout-შია. არგუმენტები ArgumentList-ით გადაეცემა, ამიტომ ჰარის შემცველი გზა ერთ
//არგუმენტად რჩება; სტანდარტული შესასვლელი დახურულია, რომ პროცესმა არაფერი იკითხოს
internal sealed class DotnetPackageUpdaterCommands : IPackageUpdaterCommands
{
    private const string Dotnet = "dotnet";

    public Result RestoreForOutdated(string solutionFileFullName)
    {
        return Run(CreateRestoreForOutdatedArguments(solutionFileFullName));
    }

    public Result UpdateOutdatedPackages(string projectFolderName)
    {
        return Run(CreateUpdateOutdatedPackagesArguments(projectFolderName));
    }

    public Result RestoreAfterOutdated(string solutionFileFullName)
    {
        return Run(CreateRestoreAfterOutdatedArguments(solutionFileFullName));
    }

    //dotnet outdated ანალიზისას თითო პროექტზე პარალელურად უშვებს "dotnet msbuild <proj> /p:NoWarn=NU1605
    // /p:TreatWarningsAsErrors=false /t:Restore,GenerateRestoreGraphFile"-ს. იმავე თვისებებით წინასწარი restore-ის
    //შემდეგ ეს გაშვებები no-op-ია და საერთო obj-ფაილებზე აღარ ეჯახება (თვისებების გარეშე restore-ისას NuGet-ის dgspec
    //ჰეში განსხვავდება და restore ხელახლა კეთდება)
    internal static string[] CreateRestoreForOutdatedArguments(string solutionFileFullName)
    {
        return ["restore", solutionFileFullName, "-p:NoWarn=NU1605", "-p:TreatWarningsAsErrors=false"];
    }

    //-n (--no-restore): CPM-ის პროექტებში dotnet outdated ვერსიებს Directory.Packages.props-ში თავად წერს. მის გარეშე
    //ის თითო პროექტსა და პაკეტზე dotnet add package-ს უშვებდა restore-ის შემოწმებით და restore-ით: SystemTools-ის 18
    //ვერსიისთვის 147 გაშვება, 262 წამი 10-ის ნაცვლად, იგივე ვერსიებით. ფაილის BOM-ს შლის (PackageUpdaterToolAction
    //აბრუნებს), ხოლო გამოტოვებულ restore-ს CreateRestoreAfterOutdatedArguments ცვლის
    internal static string[] CreateUpdateOutdatedPackagesArguments(string projectFolderName)
    {
        return ["outdated", "-r", "-u", "-n", projectFolderName];
    }

    //dotnet outdated -n-ით ჩაწერილი ვერსიების შემოწმება: ჩვეულებრივი restore პროექტების საკუთარი თვისებებით, როგორც
    //build-ისას. ცვლის -n-ით გამოტოვებულ dotnet add package-ის restore-ს, ოღონდ ერთხელ სოლუშენზე
    internal static string[] CreateRestoreAfterOutdatedArguments(string solutionFileFullName)
    {
        return ["restore", solutionFileFullName];
    }

    internal static ProcessStartInfo CreateStartInfo(string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            //dotnet PATH-იდან, როგორც SupportTools-ის სხვა dotnet ბრძანებებში
#pragma warning disable S4036
            FileName = Dotnet,
#pragma warning restore S4036
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        //MSBuild-ის ხელახლა გამოსაყენებელი node-ები (restore-ის და dotnet outdated-ის msbuild-ის) build-ის შემდეგაც
        //ცოცხლობს და გამოტანის pipe-ს ღიად ტოვებს, ამიტომ მისი ბოლოს მოლოდინი (WaitForExit) წუთობით ჩერდებოდა. node-ები
        //ბრძანების ბოლოს უნდა დაიხუროს
        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        return startInfo;
    }

    private static Result Run(string[] arguments)
    {
        using var process = new Process();
        process.StartInfo = CreateStartInfo(arguments);
        var output = new StringBuilder();
        var outputLock = new Lock();
        //stdout და stderr ასინქრონულად იკითხება, რომ სავსე pipe-ზე პროცესი არ გაიჭედოს
        process.OutputDataReceived += (_, e) => Append(output, outputLock, e.Data);
        process.ErrorDataReceived += (_, e) => Append(output, outputLock, e.Data);
        process.Start();
        process.StandardInput.Close();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        //უპარამეტრო WaitForExit გამოტანის ასინქრონული კითხვის დასრულებასაც ელოდება
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            return Result.Success();
        }

        return Error.Problem("DotnetCommandFailed",
            $"{Dotnet} {string.Join(' ', arguments)} failed (ExitCode={process.ExitCode}){Environment.NewLine}{output.ToString().TrimEnd()}");
    }

    private static void Append(StringBuilder output, Lock outputLock, string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (outputLock)
        {
            output.AppendLine(line);
        }
    }
}
