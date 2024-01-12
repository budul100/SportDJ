using Prism.Mvvm;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public abstract class CellViewModel
        : BindableBase
    {
        #region Private Fields

        private int column;
        private int columnSpan;
        private int row;
        private int rowSpan;

        #endregion Private Fields

        #region Protected Constructors

        protected CellViewModel(Cell cell)
        {
            Row = cell.Row;
            Column = cell.Column;

            RowSpan = 1;
            ColumnSpan = 1;
        }

        #endregion Protected Constructors

        #region Public Properties

        public int Column
        {
            get { return column; }
            set { SetProperty(ref column, value); }
        }

        public int ColumnSpan
        {
            get { return columnSpan; }
            set { SetProperty(ref columnSpan, value); }
        }

        public bool IsVisible { get; set; }

        public int Row
        {
            get { return row; }
            set { SetProperty(ref row, value); }
        }

        public int RowSpan
        {
            get { return rowSpan; }
            set { SetProperty(ref rowSpan, value); }
        }

        #endregion Public Properties
    }
}