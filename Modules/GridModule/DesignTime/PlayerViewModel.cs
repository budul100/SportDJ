using System;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.GridModule.DesignTime
{
    public class PlayerViewModel
        : ViewModels.PlayerViewModel
    {
        #region Private Fields

        private readonly bool isActive;
        private readonly bool isPlaying;

        #endregion Private Fields

        #region Public Constructors

        public PlayerViewModel()
            : this(name: "Player", row: 0, column: 0)
        { }

        public PlayerViewModel(string name, int row, int column, string title = default, string artist = default,
            TimeSpan? remaining = default)
            : base(player: GetPlayer(row: row, column: column, name: name), playerService: GetPlayerService())
        {
            Name = name;

            isPlaying = name.EndsWith("00");
            isActive = name.EndsWith("11");
        }

        #endregion Public Constructors

        #region Public Properties

        public override bool IsActive => isActive;

        public override bool IsPlaying => isPlaying;

        #endregion Public Properties

        #region Private Methods

        private static Player GetPlayer(int row, int column, string name)
        {
            var result = new Player
            {
                Column = column,
                KeyCode = default,
                Name = name,
                Row = row,
            };

            return result;
        }

        private static PlayerService GetPlayerService()
        {
            var track = new Track(
                path: default,
                title: "Title",
                artist: "Artist",
                info: "Info",
                duration: new TimeSpan(0, 3, 6));

            var result = new PlayerService
            {
                Track = track,
            };

            return result;
        }

        #endregion Private Methods
    }
}