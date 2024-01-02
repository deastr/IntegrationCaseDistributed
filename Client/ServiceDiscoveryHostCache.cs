using System.Collections.Concurrent;

namespace Client;

internal static class ServiceDiscoveryHostCache
{
    public static readonly ConcurrentBag<ServiceDiscoveryHost> HostList =
    [
        new("localhost", 9001),
        new("localhost", 9002),
        new("localhost", 9003)
    ];
}
