namespace SportDJ.Commons.Assets;

public static class Texts
{
    #region Public Fields

    public const string AppCloseHeader = "Close application";
    public const string AppCloseMessage = "Shall the application be closed?";

    public const string AppName = nameof(SportDJ);

    public const string CellGroupHeader = "Group player";
    public const string CellGroupMessage = "Shall the player be put into a new group?";
    public const string CellRemoveHeader = "Remove element";
    public const string CellRemoveMessage = "Shall the element be removed?";
    public const string CellRemoveTabMessage = "Shall the element be removed? It is the last player of its tab, so the tab is removed as well.";

    public const string PlayerClearHeader = "Clear player";
    public const string PlayerClearMessage = "Shall all files of the player be removed?";
    public const string PlayerSelectionFileHeader = "Select file";
    public const string PlayerSelectionFolderHeader = "Select folder";

    public const string SettingsLoadHeader = "Load settings";
    public const string SettingsNewHeader = "New session";
    public const string SettingsNewMessage = "Shall an empty session be started? The current session stays in its file. Next you choose where the new settings file is stored, the audio files stay where they are and are linked relative to it.";
    public const string SettingsOverwriteHeader = "Overwrite file";
    public const string SettingsOverwriteMessage = "The file {0} already exists. Shall it be overwritten?";
    public const string SettingsSaveHeader = "Save settings";

    public const string SplashCancellingMessage = "Cancelling ...";
    public const string SplashLoadingMessage = "Loading ...";

    public const string StackShuffleHeader = "Shuffle tracks";
    public const string StackShuffleMessage = "Shall the player tracks be shuffled?";

    public const string TabRemoveHeader = "Remove tab";
    public const string TabRemoveMessage = "Shall the tab and all of its players be removed?";

    #endregion Public Fields
}
