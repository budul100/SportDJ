using Avalonia.Controls;
using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Interfaces
{
    public interface IPanelService
    {
        #region Public Properties

        Control View { get; }

        PanelType Type { get; }

        #endregion Public Properties

        #region Public Methods

        void Register(PanelType type, Control view);

        #endregion Public Methods
    }
}