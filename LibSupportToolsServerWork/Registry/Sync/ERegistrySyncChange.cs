namespace LibSupportToolsServerWork.Registry.Sync;

//Pull-ისა და Push-ის სახე: რა მოუვა ჩანაწერს მიმღებ მხარეს
public enum ERegistrySyncChange
{
    None, //InSync, Conflict და Skipped
    Add, //ჩანაწერი მიმღებ მხარეს ჯერ არ არის
    Update, //ჩანაწერი ორივე მხარესაა და მიმღები მხარე იცვლება
    Delete //ჩანაწერი გამგზავნ მხარეს წაიშალა და მიმღებ მხარესაც იშლება
}
