using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Prism.DryIoc;

namespace SportDJ.Shareds.ViewModelLocator
{
    public static class ViewModelLocator
    {
        #region Public Fields

        public static readonly AttachedProperty<bool> AutoWireViewModelProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>(
                name: "AutoWireViewModel",
                ownerType: typeof(ViewModelLocator));

        #endregion Public Fields

        #region Public Constructors

        static ViewModelLocator()
        {
            AutoWireViewModelProperty.Changed.AddClassHandler<Control>(OnAutoWireViewModelChanged);
        }

        #endregion Public Constructors

        #region Public Methods

        public static bool GetAutoWireViewModel(Control control)
        {
            return control.GetValue(AutoWireViewModelProperty);
        }

        public static void SetAutoWireViewModel(Control control, bool value)
        {
            control.SetValue(AutoWireViewModelProperty, value);
        }

        #endregion Public Methods

        #region Private Methods

        private static void OnAutoWireViewModelChanged(Control control, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool autoWire
                && autoWire)
            {
                if (Design.IsDesignMode || control.DataContext != default)
                {
                    return;
                }

                var viewType = control.GetType();
                var viewModelTypeName = viewType.FullName
                    .Replace("View", "ViewModel");

                var viewModelType = viewType.Assembly
                    .GetTypes()
                    .FirstOrDefault(t => t.FullName == viewModelTypeName);

                if (viewModelType != default
                    && Application.Current is PrismApplication prismApp)
                {
                    var viewModel = prismApp.Container.Resolve(viewModelType);
                    control.DataContext = viewModel;
                }
            }
        }

        #endregion Private Methods
    }
}