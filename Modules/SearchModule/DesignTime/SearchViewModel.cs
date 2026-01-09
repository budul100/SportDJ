using System;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Modules.SearchModule.DesignTime
{
    public class SearchViewModel
        : ViewModels.SearchViewModel
    {
        #region Public Constructors

        public SearchViewModel()
            : base(default)
        {
            var random = new Random();

            for (var i = 1; i <= 10; i++)
            {
                var duration = TimeSpan.FromSeconds(random.Next(10, 480));

                var track = new Track("test", "test")
                {
                    Title = $"Example Track {i}",
                    Info = $"Sample Artist - Sample Song",
                    Duration = duration.Add(TimeSpan.FromSeconds(i * 10)),
                };

                var entry = new Entry(
                    track: track,
                    selector: default);

                Entries.Add(entry);
            }
        }

        #endregion Public Constructors
    }
}