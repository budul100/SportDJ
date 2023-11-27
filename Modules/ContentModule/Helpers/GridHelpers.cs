using Avalonia;
using Avalonia.Controls;
using System;
using System.Linq;

namespace SportDJ.Modules.ContentModule.Helpers
{
    public class GridHelpers
    {
        #region Public Fields

        /// <summary>
        /// Adds the specified number of Columns to ColumnDefinitions.
        /// Default Width is Auto
        /// </summary>
        public static readonly StyledProperty<int> ColumnCountProperty = AvaloniaProperty.Register<Grid, int>(
            name: "ColumnCount",
            defaultValue: -1);

        /// <summary>
        /// Adds the specified number of Rows to RowDefinitions.
        /// Default Height is Auto
        /// </summary>
        public static readonly StyledProperty<int> RowCountProperty = AvaloniaProperty.Register<Grid, int>(
            name: "RowCount",
            defaultValue: -1);

        /// <summary>
        /// Makes the specified Column's Width equal to Star.
        /// Can set on multiple Columns
        /// </summary>
        public static readonly StyledProperty<string> StarColumnsProperty = AvaloniaProperty.Register<Grid, string>(
            name: "StarColumns",
            defaultValue: string.Empty);

        /// <summary>
        /// Makes the specified Row's Height equal to Star.
        /// Can set on multiple Rows
        /// </summary>
        public static readonly StyledProperty<string> StarRowsProperty = AvaloniaProperty.Register<Grid, string>(
            name: "StarRows",
            defaultValue: string.Empty);

        #endregion Public Fields

        #region Public Constructors

        static GridHelpers()
        {
            ColumnCountProperty.Changed.Subscribe(x => ColumnCountChanged(
                obj: x.Sender,
                e: x));

            RowCountProperty.Changed.Subscribe(x => RowCountChanged(
                obj: x.Sender,
                e: x));

            StarColumnsProperty.Changed.Subscribe(x => StarColumnsChanged(
                obj: x.Sender,
                e: x));

            StarRowsProperty.Changed.Subscribe(x => StarRowsChanged(
                obj: x.Sender,
                e: x));
        }

        #endregion Public Constructors

        #region Public Methods

        public static void ColumnCountChanged(AvaloniaObject obj, AvaloniaPropertyChangedEventArgs e)
        {
            if (obj is Grid grid
                && (int)e.NewValue >= 0)
            {
                grid.ColumnDefinitions.Clear();

                for (var index = 0; index < (int)e.NewValue; index++)
                {
                    var definition = new ColumnDefinition()
                    {
                        Width = GridLength.Auto
                    };

                    grid.ColumnDefinitions.Add(definition);
                }

                SetStarColumns(grid);
            }
        }

        public static int GetColumnCount(AvaloniaObject obj)
        {
            return (int)obj.GetValue(ColumnCountProperty);
        }

        public static int GetRowCount(AvaloniaObject obj)
        {
            return (int)obj.GetValue(RowCountProperty);
        }

        public static string GetStarColumns(AvaloniaObject obj)
        {
            return (string)obj.GetValue(StarColumnsProperty);
        }

        public static string GetStarRows(AvaloniaObject obj)
        {
            return (string)obj.GetValue(StarRowsProperty);
        }

        public static void RowCountChanged(AvaloniaObject obj, AvaloniaPropertyChangedEventArgs e)
        {
            if (obj is Grid grid
                && (int)e.NewValue >= 0)
            {
                grid.RowDefinitions.Clear();

                for (var index = 0; index < (int)e.NewValue; index++)
                {
                    var definition = new RowDefinition()
                    {
                        Height = GridLength.Auto
                    };

                    grid.RowDefinitions.Add(definition);
                }

                SetStarRows(grid);
            }
        }

        public static void SetColumnCount(AvaloniaObject obj, int value)
        {
            obj.SetValue(ColumnCountProperty, value);
        }

        public static void SetRowCount(AvaloniaObject obj, int value)
        {
            obj.SetValue(RowCountProperty, value);
        }

        public static void SetStarColumns(AvaloniaObject obj, string value)
        {
            obj.SetValue(StarColumnsProperty, value);
        }

        public static void SetStarRows(AvaloniaObject obj, string value)
        {
            obj.SetValue(StarRowsProperty, value);
        }

        public static void StarColumnsChanged(AvaloniaObject obj, AvaloniaPropertyChangedEventArgs e)
        {
            if (obj is Grid grid
                && !string.IsNullOrEmpty(e.NewValue.ToString()))
            {
                SetStarColumns(grid);
            }
        }

        public static void StarRowsChanged(AvaloniaObject obj, AvaloniaPropertyChangedEventArgs e)
        {
            if (obj is Grid grid
                && !string.IsNullOrEmpty(e.NewValue.ToString()))
            {
                SetStarRows(grid);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static void SetStarColumns(Grid grid)
        {
            var starColumns = GetStarColumns(grid).Split(',');

            for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
            {
                if (starColumns.Contains(i.ToString()))
                {
                    grid.ColumnDefinitions[i].Width = new GridLength(
                        value: 1,
                        type: GridUnitType.Star);
                }
            }
        }

        private static void SetStarRows(Grid grid)
        {
            var starRows = GetStarRows(grid).Split(',');

            for (int i = 0; i < grid.RowDefinitions.Count; i++)
            {
                if (starRows.Contains(i.ToString()))
                {
                    grid.RowDefinitions[i].Height = new GridLength(
                        value: 1,
                        type: GridUnitType.Star);
                }
            }
        }

        #endregion Private Methods
    }
}