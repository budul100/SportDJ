using SportDJ.Commons.Models;

namespace SportDJ.Commons.Interfaces
{
    public interface IContentService
    {
        #region Public Methods

        Content GetContent();

        void Load();

        #endregion Public Methods
    }
}