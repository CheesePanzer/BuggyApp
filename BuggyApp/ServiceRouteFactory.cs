using Microsoft.Extensions.DependencyInjection;

namespace BuggyApp;

public sealed class ServiceRouteFactory : RouteFactory
{
    private readonly IServiceProvider _services;
    private readonly Type _pageType;

    public ServiceRouteFactory(IServiceProvider services, Type pageType)
    {
        _services = services;
        _pageType = pageType;
    }

    public override Element GetOrCreate() =>
        (Element)ActivatorUtilities.CreateInstance(_services, _pageType);

    public override Element GetOrCreate(IServiceProvider services) =>
        (Element)ActivatorUtilities.CreateInstance(_services, _pageType);
}
