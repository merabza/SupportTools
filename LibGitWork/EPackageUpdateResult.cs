namespace LibGitWork;

//ერთი git-ის პაკეტების განახლების შედეგი Update Outdated Packages-ის ბოლო ანგარიშისთვის
public enum EPackageUpdateResult
{
    //განახლდა, ან განსაახლებელი არაფერი იყო
    Success,

    //dotnet outdated ყველა ცდაზე ჩავარდა, ან git-ის პარამეტრები ვერ შეიქმნა
    OutdatedFailed,

    //ვერსიები ჩაიწერა, მაგრამ სოლუშენის restore მათით ჩავარდა
    RestoreAfterUpdateFailed
}
