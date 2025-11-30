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
            // Erstellt 10 Beispiel-Tracks
            var duration = TimeSpan.FromSeconds(255); // 4:15

            for (int i = 1; i <= 10; i++)
            {
                var track = new Track("test", "test")
                {
                    Title = $"Beispiel-Track {i}",
                    Info = $"Künstler: The Sample Band - Album: Design Time Hits",
                    Duration = duration.Add(TimeSpan.FromSeconds(i * 10)),
                };

                Tracks.Add(track);
            }
        }

        #endregion Public Constructors
    }
}