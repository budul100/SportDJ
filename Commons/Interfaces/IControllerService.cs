using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Records;

namespace SportDJ.Commons.Interfaces;

public interface IControllerService
    : IDisposable
{
    #region Public Properties

    Controller Controller { get; }

    IEnumerable<Controller> Controllers { get; }

    #endregion Public Properties

    #region Public Methods

    public void Colorize(Key key, int colorValue);

    /// <summary>
    /// Lights the key in the given color, as far as the profile of the controller defines it.
    /// </summary>
    void Colorize(Key key, PlayerColorType colorType);

    Task<Key> ListenAsync(CancellationToken cancellationToken);

    void Register(Key key, StrokeType strokeType, Action action);

    void Set(string name);

    void Unregister(Key key);

    void Update();

    #endregion Public Methods
}
