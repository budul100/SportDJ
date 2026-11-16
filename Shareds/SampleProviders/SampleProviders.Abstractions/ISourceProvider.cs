namespace SportDJ.Shareds.SampleProviders.Abstractions;

public interface ISourceProvider
    : IPlayingProvider
{
    #region Public Methods

    void Initialize(string filePath);

    #endregion Public Methods
}