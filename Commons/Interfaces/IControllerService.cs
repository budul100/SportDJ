using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SportDJ.Commons.Models.Records;

namespace SportDJ.Commons.Interfaces
{
    public interface IControllerService
        : IDisposable
    {
        #region Public Properties

        Controller Controller { get; }

        IEnumerable<Controller> Controllers { get; }

        #endregion Public Properties

        #region Public Methods

        public void Colorize(Key key, int colorValue);

        Task<Key> ListenAsync(CancellationToken cancellationToken);

        void Register(Key key, Action action);

        void Set(string name);

        void Update();

        #endregion Public Methods
    }
}