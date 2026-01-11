using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace SportDJ.Commons.Extensions
{
    public static class ListBoxExtensions
    {
        #region Public Fields

        public static readonly AttachedProperty<bool> AutoScrollProperty = AvaloniaProperty
            .RegisterAttached<ListBox, bool>(
                name: "AutoScroll",
                ownerType: typeof(ListBoxExtensions));

        #endregion Public Fields

        #region Public Constructors

        static ListBoxExtensions()
        {
            AutoScrollProperty.Changed.AddClassHandler<ListBox>(OnAutoScrollChanged);
        }

        #endregion Public Constructors

        #region Public Methods

        public static bool GetAutoScroll(ListBox element) => element.GetValue(
            property: AutoScrollProperty);

        public static void SetAutoScroll(ListBox element, bool value) => element.SetValue(
            property: AutoScrollProperty,
            value: value);

        #endregion Public Methods

        #region Private Methods

        private static void OnAutoScrollChanged(ListBox listBox, AvaloniaPropertyChangedEventArgs args)
        {
            if ((bool)args.NewValue)
            {
                listBox.PropertyChanged += OnPropertyChanged;
            }
            else
            {
                listBox.PropertyChanged -= OnPropertyChanged;
            }
        }

        private static void OnPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty
                && sender is ListBox lb)
            {
                ScrollToSelectedDispatcher(lb);
            }
        }

        private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb)
            {
                ScrollToSelectedDispatcher(lb);
            }
        }

        private static void ScrollToSelected(ListBox listBox)
        {
            if (listBox.SelectedItem != null)
            {
                try
                {
                    listBox.ScrollIntoView(listBox.SelectedItem);
                }
                catch
                { }
            }
        }

        private static void ScrollToSelectedDispatcher(ListBox listBox)
        {
            Dispatcher.UIThread.Post(
                action: () => ScrollToSelected(listBox),
                priority: DispatcherPriority.Background);
        }

        #endregion Private Methods
    }
}