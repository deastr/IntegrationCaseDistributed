namespace Client;

internal class ServiceDiscoveryMessageHandler : DelegatingHandler
{
    private readonly IServiceDiscoveryHostSelector serviceDiscoveryHostSelector;

    public ServiceDiscoveryMessageHandler(IServiceDiscoveryHostSelector serviceDiscoveryHostSelector)
    {
        this.serviceDiscoveryHostSelector = serviceDiscoveryHostSelector ?? throw new ArgumentNullException(nameof(serviceDiscoveryHostSelector));

        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var newUri = GetHostFromServiceDiscovery(request.RequestUri!);
        request.RequestUri = newUri;
        return await base.SendAsync(request, ct);
    }

    private Uri GetHostFromServiceDiscovery(Uri uri)
    {
        var host = serviceDiscoveryHostSelector.SelectHost(uri.Host);
        var newUri = new UriBuilder(uri)
        {
            Host = host.Address,
            Port = host.Port
        };
        return newUri.Uri;
    }
}
