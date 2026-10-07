namespace LibSupportToolsServerWork.Registry.Sync;

//კონფლიქტის სახე
public enum ERegistrySyncConflict
{
    None,
    BothChanged, //ორივე მხარე შეიცვალა და შიგთავსი განსხვავდება
    DeletedOnServer, //სერვერზე წაიშალა, ლოკალურად კი შეიცვალა
    DeletedLocally, //ლოკალურად წაიშალა, სერვერზე კი შეიცვალა
    FirstSyncDiffers, //საერთო წინა მდგომარეობა არ არის (პირველი სინქრონიზაცია ან ორივეგან დამატება) და შიგთავსი განსხვავდება
    DeleteNeedsConfirmation //ერთ მხარეს წაიშალა, მეორე არ შეცვლილა, მაგრამ ფაილების კოლექციაში წაშლას მომხმარებელი ადასტურებს
}
