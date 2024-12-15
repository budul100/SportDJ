using System.Diagnostics;
using System.Linq;

namespace SportDJ.Modules.SearchModule.DesignTime
{
    public class SearchViewModel
        : ViewModels.SearchViewModel
    {
        #region Public Constructors

        public SearchViewModel()
            : base(regionManager: default)
        {
            var track1 = new TrackViewModel(); // TrackViewModel("Track 1", "Artist 1", new System.TimeSpan(0, 1, 20));
            Tracks.Add(track1);

            var track2 = new TrackViewModel(); // new TrackViewModel("Track 2", "Artist 2", new System.TimeSpan(0, 2, 25));
            Tracks.Add(track2);

            var track3 = new TrackViewModel(); // new TrackViewModel("Track 3", "Artist 3", new System.TimeSpan(0, 3, 30));
            Tracks.Add(track3);

            RaisePropertyChanged(nameof(Tracks));

            Debug.WriteLine(Tracks.Count() + " Tracks");
        }

        #endregion Public Constructors
    }
}