using Avalonia.Media.Imaging;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Modules.SearchModule.ViewModels
{
    public class TrackViewModel(Track track)
        : BindableBase
    {
        #region Public Properties

        public virtual Bitmap Bitmap => track?.Bitmap;

        public virtual string Content => track?.Info;

        public virtual string Counter => track?.Counter > 0
            ? $"({track.Counter})"
            : default;

        public virtual string Duration => track?.Duration?.ToString(Constants.FormatDuration);

        #endregion Public Properties
    }
}