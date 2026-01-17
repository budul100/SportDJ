using Avalonia.Controls;
using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Interfaces
{
    public interface IPanelService
    {
        #region Public Methods

        void Activate<TService>();

        void Deactivate<TService>();

        Control GetView(PanelType type);

        bool IsVisible(PanelType type);

        bool IsVisible<TService>();

        void Register<TService>(PanelType type, Control view);

        #endregion Public Methods
    }
}