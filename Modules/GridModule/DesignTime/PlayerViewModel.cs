using System;
using SportDJ.Commons.Assets;

namespace SportDJ.Modules.GridModule.DesignTime
{
    public class PlayerViewModel
        : ViewModels.PlayerViewModel
    {
        #region Private Fields

        private readonly int column;
        private readonly string content;
        private readonly string counter;
        private readonly bool isActive;
        private readonly bool isPlaying;
        private readonly TimeSpan remaining;
        private readonly int row;

        #endregion Private Fields

        #region Public Constructors

        public PlayerViewModel()
            : this(name: "Player", row: 0, column: 0)
        { }

        public PlayerViewModel(string name, int row, int column, string title = "Title", string artist = "Artist",
            TimeSpan? remaining = default)
            : base(containerProvider: default, player: default, parent: default)
        {
            this.remaining = remaining ?? new TimeSpan(0, 3, 6);

            Name = name;

            this.column = column;
            this.row = row;

            content = $"{title} - {artist}";
            counter = "(1)";
            isPlaying = name.EndsWith("00");
            isActive = name.EndsWith("11");
        }

        #endregion Public Constructors

        #region Public Properties

        public override int Column => column;

        public override string Content => content;

        public override string Counter => counter;

        public override bool IsActive => isActive;

        public override bool IsPlaying => isPlaying;

        public override string Name { get; set; }

        public override string Remaining => remaining.ToString(Constants.FormatDuration);

        public override int Row => row;

        #endregion Public Properties
    }
}