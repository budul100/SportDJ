namespace SportDJ.Modules.ContentModule.ViewModels
{
    public abstract class CellViewModel
        : BaseViewModel
    {
        #region Public Properties

        public int Column { get; private set; }

        public int ColumnSpan { get; private set; }

        public int Row { get; private set; }

        public int RowSpan { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public virtual void Initialize(string name, int row, int column, int rowSpan = 1, int columnSpan = 1)
        {
            base.Initialize(name);

            this.Row = row;
            this.Column = column;

            this.RowSpan = rowSpan;
            this.ColumnSpan = columnSpan;
        }

        #endregion Public Methods
    }
}