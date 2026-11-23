using System;

namespace SportDJ.Commons.Interfaces;

/// <summary>
/// Holds the owner which currently waits for a keyboard key to be assigned.
/// Only one owner can listen at a time.
/// </summary>
public interface IKeyListenService
{
    #region Public Events

    event EventHandler Changed;

    #endregion Public Events

    #region Public Properties

    bool IsAny { get; }

    #endregion Public Properties

    #region Public Methods

    void Begin(object owner);

    void End(object owner);

    bool IsListening(object owner);

    #endregion Public Methods
}
