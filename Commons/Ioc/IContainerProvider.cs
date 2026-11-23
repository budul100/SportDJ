using System;

namespace SportDJ.Commons.Ioc;

public interface IContainerProvider
{
    #region Public Methods

    /// <summary>
    /// Resolves the (unnamed) registration for the given type.
    /// </summary>
    object Resolve(Type type);

    /// <summary>
    /// Resolves the registration with the given name for the given type.
    /// </summary>
    object Resolve(Type type, string name);

    #endregion Public Methods
}
