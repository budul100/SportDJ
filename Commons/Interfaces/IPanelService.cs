using Avalonia.Controls;
using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Interfaces
{
    public interface IPanelService
    {
        #region Public Methods

        Control GetView(PanelType type);

        bool IsVisible(PanelType type);

        void Register<TService>(PanelType type, Control view);

        void Switch<TService>(TService service = default);

        #endregion Public Methods
    }
}