using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MsBox.Avalonia.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SportDJ.Commons.Interfaces
{
    public interface IDialogService
    {
        #region Public Methods

        Task<IStorageFolder> GetFolderAsync(string basePath = default);

        Task<ButtonResult> GetMessageBoxResultAsync(string contentMessage, string contentTitle,
            ButtonEnum buttonDefinitions = ButtonEnum.YesNo, ClickEnum enterDefaultButton = ClickEnum.Yes,
            ClickEnum escDefaultButton = ClickEnum.No, Icon icon = Icon.Question, bool showInCenter = true,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner);

        Task<IEnumerable<IStorageFile>> OpenFilePickerAsync(string title,
            IReadOnlyList<FilePickerFileType> fileTypeFilter = default, bool allowMultiple = false,
            IStorageFolder startLocation = default);

        Task<IEnumerable<IStorageFolder>> OpenFolderPickerAsync(string title, bool allowMultiple = false,
            IStorageFolder startLocation = default);

        Task ShowMessageBoxAsync(string contentMessage, string contentTitle, Icon icon = Icon.Question,
            bool showInCenter = true, WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner);

        #endregion Public Methods
    }
}