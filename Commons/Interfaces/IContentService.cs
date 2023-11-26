using SportDJ.Commons.Models;

namespace SportDJ.Commons.Interfaces
{
    public interface IContentService
    {
        #region Public Properties

        Set Set { get; }

        #endregion Public Properties

        #region Public Methods

        void Load();

        #endregion Public Methods
    }
}