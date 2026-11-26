namespace LibAppInstallWork.Models;

//ფაილსაცავში ატვირთული პარამეტრები: სერვისისთვის გადასაცემი ფაილის სახელი და ფაილში ჩაწერილი ვერსია
internal sealed record UploadedAppSettings(string FileName, string? Version);
