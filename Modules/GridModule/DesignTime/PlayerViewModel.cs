using Avalonia.Media.Imaging;
using SharpHook.Native;
using SportDJ.Commons;
using System;

namespace SportDJ.Modules.GridModule.DesignTime
{
    public class PlayerViewModel
        : ViewModels.PlayerViewModel
    {
        #region Public Constructors

        public PlayerViewModel()
            : this(name: "Player", row: 0, column: 0)
        { }

        public PlayerViewModel(string name, int row, int column, string title = default, string artist = default,
            TimeSpan? remaining = default)
            : base(containerProvider: default, eventAggregator: default, trackService: default, hook: default)
        {
            this.Name = name;
            this.Row = row;
            this.Column = column;

            artist ??= "Artist";
            title ??= "Title";

            this.Content = $"{artist} - {title}";
            this.Remaining = remaining?.ToString(Constants.FormatDuration) ?? "00:05.3";
            this.Counter = $"[{3}]";
            this.Key = KeyCode.VcA;

            this.Bitmap = new Bitmap(@"C:\Users\mgr\Entwicklung\SportDJ\Additionals\Samples\Sample1.png");
        }

        #endregion Public Constructors

        #region Public Properties

        public override bool IsActive => false;

        public override bool IsPlaying => true;

        #endregion Public Properties
    }
}