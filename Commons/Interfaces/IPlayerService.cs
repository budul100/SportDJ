using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Records;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Commons.Interfaces;

public interface IPlayerService
    : IDisposable
{
    #region Public Properties

    PlayerColorType Color { get; set; }

    IEnumerable<Entry> Entries { get; }

    bool HasPaths { get; }

    bool HasPlayeds { get; }

    bool HasStack { get; }

    bool HasUnplayeds { get; }

    bool IsActive { get; }

    bool IsAvailable { get; }

    bool IsEnding { get; }

    bool IsListeningKeyboard { get; }

    bool IsListeningMidi { get; }

    /// <summary>
    /// The player waits for its files or for its signal, so it cannot be played yet.
    /// </summary>
    bool IsLoading { get; }

    bool IsOverlay { get; set; }

    bool IsPlaying { get; }

    Key Key { get; }

    string KeyCode { get; set; }

    string Name { get; set; }

    double? Progress { get; }

    TimeSpan? Remaining { get; }

    bool ResetPosition { get; set; }

    Track Track { get; }

    #endregion Public Properties

    #region Public Methods

    void CancelListening();

    Task ClearAsync();

    void Execute();

    void Initialize(Player player);

    void ListenKeyboard();

    void Next();

    void Previous();

    Task RegisterAsync();

    Task SetFileAsync();

    Task SetFolderAsync();

    void Unregister();

    #endregion Public Methods
}