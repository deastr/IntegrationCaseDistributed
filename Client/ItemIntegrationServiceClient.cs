using Shared;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Client;

internal static class ItemIntegrationServiceClient
{
    private static readonly HttpClient httpClient = new(new ServiceDiscoveryMessageHandler(new RoundRobinHostSelector()));
    private static readonly JsonSerializerOptions serializerOptions = new() { PropertyNameCaseInsensitive = true };

    static ItemIntegrationServiceClient()
    {
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
    }

    public static async Task<Result> SaveItem(ItemRequest itemContent)
    {
        HttpRequestMessage request = new()
        {
            Method = HttpMethod.Put,
            Content = JsonContent.Create(itemContent),
            RequestUri = new Uri("http://localhost/saveItem")
        };
        request.Headers.Add("Idempotency-Key", itemContent.Content);

        var response = await httpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return new(true, "Resent request");
        }

        response.EnsureSuccessStatusCode();

        var item = JsonSerializer.Deserialize<Item>(await response.Content.ReadAsStringAsync(), serializerOptions);
        return new(Success: true, Message: item!.ToString());
    }

    public static async Task<List<Item>> GetAllItems()
    {
        HttpRequestMessage request = new()
        {
            RequestUri = new Uri("http://localhost/getAllItems")
        };

        var response = await httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<List<Item>>(await response.Content.ReadAsStringAsync())!;
    }
}
