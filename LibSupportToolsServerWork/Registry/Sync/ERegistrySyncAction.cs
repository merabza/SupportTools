namespace LibSupportToolsServerWork.Registry.Sync;

//ჩანაწერის მოქმედება სინქრონიზაციის გეგმაში
public enum ERegistrySyncAction
{
    InSync, //გადასატანი არაფერია; შემსრულებელი საჭიროებისას მხოლოდ მდგომარეობას ანახლებს
    Pull, //სერვერის ცვლილება ლოკალურად ჩამოდის
    Push, //ლოკალური ცვლილება სერვერზე მიდის
    Conflict, //ორივე მხარე შეიცვალა; მომხმარებელი ირჩევს: Local, Server ან Skip
    Skipped //ჩანაწერი ExcludedKeys-შია და სინქრონიზაციაში არ მონაწილეობს
}
