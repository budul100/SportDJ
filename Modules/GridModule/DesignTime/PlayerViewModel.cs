using System;
using System.IO;
using Avalonia.Media.Imaging;
using SportDJ.Commons.Assets;

namespace SportDJ.Modules.GridModule.DesignTime;

public class PlayerViewModel(string name, int row, int column, string title = "Title", string artist = "Artist",
    TimeSpan? remaining = default)
    : ViewModels.PlayerViewModel(containerProvider: default, player: default, parent: default)
{
    #region Private Fields

    private readonly TimeSpan remaining = remaining ?? new TimeSpan(0, 3, 6);

    #endregion Private Fields

    #region Public Constructors

    public PlayerViewModel()
        : this(name: "Player", row: 0, column: 0)
    { }

    #endregion Public Constructors

    #region Public Properties

    public override Bitmap Bitmap { get; } = LoadSample();

    public override int Column => column;

    public override string Counter { get; } = "(1)";

    public override string Info { get; } = $"{title} - {artist}";

    public override bool IsActive { get; } = name.EndsWith("11");

    public override bool IsPlaying { get; } = name.EndsWith("00");

    public override string Name { get; set; } = name;

    public override string Remaining => remaining.ToString(Constants.FormatDuration);

    public override int Row => row;

    #endregion Public Properties

    #region Private Methods

    private static Bitmap LoadSample()
    {
        // Relative to the application directory instead of the working directory,
        // and without exception if the sample is missing
        var path = Path.Combine(AppContext.BaseDirectory, "Additionals", "Samples", "Sample1.png");

        return File.Exists(path)
            ? new Bitmap(path)
            : default;
    }

    #endregion Private Methods
}
