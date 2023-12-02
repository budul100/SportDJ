using SportDJ.Commons.Models.Contents;

namespace SportDJ.Modules.ContentModule.DesignTime
{
    public class ControllerViewModel
        : ViewModels.ControllerViewModel
    {
        #region Public Constructors

        public ControllerViewModel()
            : this("Player", 0, 0)
        { }

        public ControllerViewModel(string name, int row, int column)
            : base(default, default)
        {
            Initialize(new Controller { Name = name, Row = row, Column = column });
        }

        #endregion Public Constructors
    }
}