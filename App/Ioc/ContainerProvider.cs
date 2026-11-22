using System;
using Microsoft.Extensions.DependencyInjection;
using SportDJ.Commons.Ioc;

namespace SportDJ.App.Ioc;

/// <summary>
/// Resolves services from the built service provider. Types registered as transient are created here
/// and not by the service provider, which would keep every disposable transient alive until shutdown.
/// Concrete types without registration (views, view models) are created on demand.
/// </summary>
public class ContainerProvider(ContainerRegistry registry)
    : IContainerProvider, IServiceProvider
{
    #region Private Fields

    private IServiceProvider serviceProvider;

    #endregion Private Fields

    #region Public Methods

    public object GetService(Type serviceType)
    {
        return registry.Transients.TryGetValue(serviceType, out var implementationType)
            ? ActivatorUtilities.CreateInstance(this, implementationType)
            : serviceProvider.GetService(serviceType);
    }

    public void Initialize(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public object Resolve(Type type)
    {
        var result = GetService(type);

        if (result == default
            && type.IsClass
            && !type.IsAbstract)
        {
            result = ActivatorUtilities.CreateInstance(this, type);
        }

        return result
            ?? throw new InvalidOperationException($"No registration found for type {type.FullName}.");
    }

    public object Resolve(Type type, string name)
    {
        return serviceProvider.GetRequiredKeyedService(
            serviceType: type,
            serviceKey: name);
    }

    #endregion Public Methods
}
