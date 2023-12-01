using Prism.Mvvm;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;

namespace SportDJ.App.ViewModels
{
    public class MainViewModel
        : BindableBase
    {
        #region Public Constructors

        public MainViewModel(ISettingsService<Session> settingsService)
        {
            var session = settingsService.Settings;

            session.Set.Contents.Clear();

            session.Set.Contents.Add(new Player() { Name = "Player 1", Row = 0, Column = 0 });

            session.Set.Contents.Add(new Player() { Name = "Player 2", Row = 1, Column = 0 });

            session.Set.Contents.Add(new Player() { Name = "Player 3", Row = 0, Column = 1 });

            var group = new Group
            {
                Name = "Group1",
                Row = 0,
                Column = 2,
                Rows = 2,
                Columns = 2,
            };

            group.Contents.Add(new Player() { Name = "Player 21", Row = 0, Column = 0 });

            group.Contents.Add(new Player() { Name = "Player 22", Row = 1, Column = 0 });

            group.Contents.Add(new Player() { Name = "Player 23", Row = 0, Column = 1 });

            group.Contents.Add(new Player() { Name = "Player 24", Row = 1, Column = 1 });

            session.Set.Contents.Add(group);

            session.Set.Rows = 2;
            session.Set.Columns = 4;

            settingsService.Save();
        }

        #endregion Public Constructors
    }
}