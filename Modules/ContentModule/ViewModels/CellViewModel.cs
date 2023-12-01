using Prism.Mvvm;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public abstract class CellViewModel
        : BindableBase
    {
        #region Private Fields

        private int column;
        private int columnSpan;
        private string name;
        private int row;
        private int rowSpan;

        #endregion Private Fields

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

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

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

        #region Public Methods

        public virtual void Initialize<T>(T cell, int rowSpan = 1, int columnSpan = 1)
            where T : Cell
        {
            this.Name = cell.Name;

            this.Row = cell.Row;
            this.Column = cell.Column;

            this.RowSpan = rowSpan;
            this.ColumnSpan = columnSpan;
        }

        #endregion Public Methods
    }
}