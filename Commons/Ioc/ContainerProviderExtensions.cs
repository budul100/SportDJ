namespace SportDJ.Commons.Ioc;

public static class ContainerProviderExtensions
{
    #region Public Methods

    public static T Resolve<T>(this IContainerProvider containerProvider)
    {
        return (T)containerProvider.Resolve(typeof(T));
    }

    public static T Resolve<T>(this IContainerProvider containerProvider, string name)
    {
        return (T)containerProvider.Resolve(typeof(T), name);
    }

    #endregion Public Methods
}
