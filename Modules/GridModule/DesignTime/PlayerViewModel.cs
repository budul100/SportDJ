using Avalonia.Media.Imaging;
using SharpHook.Native;
using SportDJ.Commons.Assets;
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
            : base(containerProvider: default, player: default, parent: default)
        {
            Name = name;
            Row = row;
            Column = column;

            artist ??= "Artist";
            title ??= "Title";

            Content = $"{artist} - {title}";
            Remaining = remaining?.ToString(Constants.FormatDuration) ?? "00:05.3";
            Counter = $"[{3}]";
            Key = KeyCode.VcA;

            Bitmap = new Bitmap(@"C:\Users\mgr\Entwicklung\SportDJ\Additionals\Samples\Sample1.png");

            WidthPosition = 20;
            WidthRemaining = 80;
        }

        #endregion Public Constructors

        #region Public Properties

        public override bool IsActive => true;

        public override bool IsPlaying => true;

        #endregion Public Properties
    }
}