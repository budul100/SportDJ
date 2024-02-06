using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using PropertyModels.Extensions;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<IStorageFolder> GetFolderAsync(IEnumerable<string> paths = default,
            Environment.SpecialFolder? alternative = Environment.SpecialFolder.UserProfile)
        {
            var result = default(IStorageFolder);

            var relevant = default(string);

            if (paths?.Any() == true)
            {
                relevant = paths?.FirstOrDefault(p => Directory.Exists(p));

                if (string.IsNullOrEmpty(relevant))
                {
                    relevant = paths?
                        .Select(p => Path.GetDirectoryName(p))
                        .FirstOrDefault(p => Directory.Exists(p));
                }
            }

            if (string.IsNullOrEmpty(relevant)
                && alternative != default)
            {
                relevant = Environment.GetFolderPath(alternative.Value);
            }

            if (Directory.Exists(relevant))
            {
                var topLevel = GetTopLevel();

                result = await topLevel?.StorageProvider.TryGetFolderFromPathAsync(relevant);
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

            var uri = new Uri($"avares://{Texts.AppName}/Assets/{Texts.AppName}.ico");
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