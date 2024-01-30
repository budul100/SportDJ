using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Interfaces;

namespace SportDJ.DialogService
{
    public class Service
        : IDialogService
    {
        #region Private Fields

        private readonly IClassicDesktopStyleApplicationLifetime desktop;
        private readonly WindowIcon windowIcon;

        #endregion Private Fields

        #region Public Constructors

        public Service(Application application)
        {
            if (application is null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            desktop = application?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            windowIcon = GetWindowIcon();
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<IStorageFolder> GetFolderAsync(string basePath = default)
        {
            var result = default(IStorageFolder);

            var folderPath = Path.GetDirectoryName(basePath);

            if (Directory.Exists(folderPath))
            {
                var topLevel = GetTopLevel();

                result = await topLevel?.StorageProvider.TryGetFolderFromPathAsync(folderPath);
            }

            return result;
        }

        public async Task<ButtonResult> GetMessageBoxResultAsync(string contentMessage, string contentTitle,
            ButtonEnum buttonDefinitions = ButtonEnum.YesNo, ClickEnum enterDefaultButton = ClickEnum.Yes,
            ClickEnum escDefaultButton = ClickEnum.No, Icon icon = Icon.Question, bool showInCenter = true,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner)
        {
            var messageBoxParams = new MessageBoxStandardParams
            {
                ButtonDefinitions = buttonDefinitions,
                ContentMessage = contentMessage,
                ContentTitle = contentTitle,
                EnterDefaultButton = enterDefaultButton,
                EscDefaultButton = escDefaultButton,
                Icon = icon,
                ShowInCenter = showInCenter,
                WindowStartupLocation = windowStartupLocation,
                WindowIcon = windowIcon,
            };

            var dialog = MessageBoxManager.GetMessageBoxStandard(messageBoxParams);

            var result = await dialog.ShowWindowDialogAsync(desktop.MainWindow);

            return result;
        }

        public async Task<IEnumerable<IStorageFile>> OpenFilePickerAsync(string title,
            IReadOnlyList<FilePickerFileType> fileTypeFilter = default, bool allowMultiple = false,
            IStorageFolder startLocation = default)
        {
            var options = new FilePickerOpenOptions
            {
                AllowMultiple = allowMultiple,
                FileTypeFilter = fileTypeFilter,
                SuggestedStartLocation = startLocation,
                Title = title,
            };

            var topLevel = GetTopLevel();

            var result = await topLevel?.StorageProvider.OpenFilePickerAsync(options);

            return result;
        }

        public async Task<IEnumerable<IStorageFolder>> OpenFolderPickerAsync(string title, bool allowMultiple = false,
            IStorageFolder startLocation = default)
        {
            var options = new FolderPickerOpenOptions
            {
                AllowMultiple = allowMultiple,
                SuggestedStartLocation = startLocation,
                Title = title,
            };

            var topLevel = GetTopLevel();

            var result = await topLevel?.StorageProvider.OpenFolderPickerAsync(options);

            return result;
        }

        public async Task ShowMessageBoxAsync(string contentMessage, string contentTitle, Icon icon = Icon.Question,
            bool showInCenter = true, WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner)
        {
            var messageBoxParams = new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.Ok,
                ContentMessage = contentMessage,
                ContentTitle = contentTitle,
                EnterDefaultButton = ClickEnum.Ok,
                EscDefaultButton = ClickEnum.Ok,
                Icon = icon,
                ShowInCenter = showInCenter,
                WindowStartupLocation = windowStartupLocation,
                WindowIcon = windowIcon,
            };

            var dialog = MessageBoxManager.GetMessageBoxStandard(messageBoxParams);

            await dialog.ShowWindowDialogAsync(desktop.MainWindow);
        }

        #endregion Public Methods

        #region Private Methods

        private static WindowIcon GetWindowIcon()
        {
            var result = default(WindowIcon);

            var uri = new Uri($"avares://{Constants.AppName}/Assets/{Constants.AppName}.ico");
            var stream = AssetLoader.Open(uri);

            if (stream != default)
            {
                var bitmap = new Bitmap(stream);
                result = new WindowIcon(bitmap);
            }

            return result;
        }

        private TopLevel GetTopLevel()
        {
            var mainWindow = desktop?.MainWindow;

            var result = mainWindow != default
                ? Window.GetTopLevel(mainWindow)
                : default;

            return result;
        }

        #endregion Private Methods
    }
}