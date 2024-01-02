using System.Collections.Concurrent;

namespace Client;

/*
 Things that are missing in this sample:
    - Centralized service discovery service (i.e. Consul, etcd, etc.)
    - Centralized host cache
    - Services self-registering on startup
    - Background service on clients for host health check
    - Different host selector strategies (random, least connected, sticky session, etc.)
*/

internal interface IServiceDiscoveryHostSelector
{
    ServiceDiscoveryHost SelectHost(string service);
}

internal class RoundRobinHostSelector : IServiceDiscoveryHostSelector
{
    private readonly ConcurrentDictionary<string, int> serviceLastIndex = new();
    private readonly object _lock = new();

    public ServiceDiscoveryHost SelectHost(string service)
    {
        int lastIndex = serviceLastIndex.GetOrAdd(service, -1);

        lock (_lock)
        {
            if (lastIndex > ServiceDiscoveryHostCache.HostList.Count || ++lastIndex > (ServiceDiscoveryHostCache.HostList.Count - 1))
            {
                lastIndex = 0;
            }

            int i = 0;
            foreach (var host in ServiceDiscoveryHostCache.HostList)
            {
                if (i == lastIndex)
                {
                    serviceLastIndex[service] = i;
                    return host!;
                }
                i++;
            }

            return ServiceDiscoveryHostCache.HostList.First();
        }
    }
}
