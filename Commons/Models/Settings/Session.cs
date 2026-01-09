using System.Collections.Generic;
using System.Xml.Serialization;
using SportDJ.Commons.Assets;

namespace SportDJ.Commons.Models.Settings
{
    public class Session
    {
        #region Public Properties

        public int Height { get; set; } = Defaults.AppHeight;

        public List<Output> Outputs { get; set; } = [];

        [XmlIgnore]
        public bool PanelVisible { get; set; }

        public double PanelWidth { get; set; } = Defaults.WidthPanel;

        public List<Position> Positions { get; set; } = [];

        public string SettingsPath { get; set; }

        public bool SettingsVisible { get; set; } = true;

        public double SettingsWidth { get; set; } = Defaults.WidthSettings;

        public int Width { get; set; } = Defaults.AppWidth;

        public string WindowState { get; set; } = nameof(Avalonia.Controls.WindowState.Maximized);

        #endregion Public Properties
    }
}