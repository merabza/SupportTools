using System;
using System.Net.Http;

namespace SupportTools.AutoSync;

//ავტომატური სინქრონიზაციის HttpClient-ები მოკლე Timeout-ით (SupportToolsServerSyncTimeoutSeconds). ApiClient Timeout-ს
//ApiRequestFailed-ად აქცევს, ანუ ძრავისთვის ეს მიუწვდომელი სერვერია: გეგმა არ იქმნება, შესრულებისას კი სერვერის
//დარჩენილი ოპერაციები აღარ სრულდება და უკვე შესრულებულების მდგომარეობა ინახება. Timeout ერთ მოთხოვნას ეხება და არა
//მთელ სინქრონიზაციას, ამიტომ ახალ კომპიუტერზე დიდი პირველი Pull ბოლომდე სრულდება
internal sealed class TimeoutHttpClientFactory : IHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeSpan _timeout;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TimeoutHttpClientFactory(IHttpClientFactory httpClientFactory, TimeSpan timeout)
    {
        _httpClientFactory = httpClientFactory;
        _timeout = timeout;
    }

    public HttpClient CreateClient(string name)
    {
        HttpClient client = _httpClientFactory.CreateClient(name);
        client.Timeout = _timeout;
        return client;
    }
}
