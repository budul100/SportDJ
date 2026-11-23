using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using SportDJ.Commons.Ioc;

namespace SportDJ.App.Ioc;

public class ContainerRegistry
{
    #region Private Fields

    private readonly ServiceCollection services = new();

    #endregion Private Fields

    #region Public Properties

    public Dictionary<Type, Type> Transients { get; } = [];

    #endregion Public Properties

    #region Public Methods

    public IContainerProvider Build()
    {
        var containerProvider = new ContainerProvider(this);

        services.AddSingleton<IContainerProvider>(containerProvider);

        containerProvider.Initialize(services.BuildServiceProvider());

        return containerProvider;
    }

    public void Register<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        Transients[typeof(TService)] = typeof(TImplementation);

        // Also known to the service provider, so singletons can get it injected
        services.AddTransient<TService>(
            provider => (TImplementation)Create(provider, typeof(TImplementation)));
    }

    public void RegisterSingleton<TService>()
        where TService : class
    {
        services.AddSingleton<TService>(
            provider => (TService)Create(provider, typeof(TService)));
    }

    public void RegisterSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        services.AddSingleton<TService>(
            provider => (TImplementation)Create(provider, typeof(TImplementation)));
    }

    public void RegisterSingleton(Type service, Type implementation, string name)
    {
        services.AddKeyedSingleton(
            serviceType: service,
            serviceKey: name,
            implementationFactory: (provider, _) => Create(provider, implementation));
    }

    public void RegisterSingleton<TService>(TService instance)
        where TService : class
    {
        services.AddSingleton(instance);
    }

    #endregion Public Methods

    #region Private Methods

    private static object Create(IServiceProvider provider, Type implementation)
    {
        return ConstructionTracer.Create(
            type: implementation,
            factory: () => ActivatorUtilities.CreateInstance(provider, implementation));
    }

    #endregion Private Methods
}
