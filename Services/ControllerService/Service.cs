using System.Collections.Generic;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Records;

namespace ControllerService
{
    public class Service
        : IControllerService
    {
        #region Private Fields

        private bool isDisposed;

        #endregion Private Fields

        #region Public Properties

        public Controller Controller => throw new System.NotImplementedException();

        public IEnumerable<Controller> Controllers => throw new System.NotImplementedException();

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                disposing: true);

            System.GC.SuppressFinalize(
                obj: this);
        }

        public void Update()
        {
            throw new System.NotImplementedException();
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    // TODO: Verwalteten Zustand (verwaltete Objekte) bereinigen
                }

                // TODO: Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
                // TODO: Große Felder auf NULL setzen
                isDisposed = true;
            }
        }

        #endregion Protected Methods
    }
}