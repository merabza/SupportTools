namespace SupportTools.Models;

//სერვერის გამოყენების ადგილი. Name სერვერის მენიუში ჩანს, MenuLink კი იმ მენიუს ბმულია (მთავარი მენიუდან მენიუს
//ჩანაწერების სახელები "/"-ით), რომელშიც პროგრამა ამ ჩანაწერის არჩევისას გადადის
public sealed record ServerUsage(string Name, string MenuLink);
