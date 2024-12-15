using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Structure;
using System.Threading.Tasks;

namespace SportDJ.Commons.Interfaces
{
    public interface IGridService
    {
        #region Public Properties

        Grid Grid { get; }

        #endregion Public Properties

        #region Public Methods

        void AddGrid(Grid grid, OrientationType orientation);

        void AddPlayer(Cell cell, OrientationType orientation);

        void MoveCell(Cell cell, OrientationType orientation);

        void MoveGrid(Grid grid, OrientationType orientation);

        Task RemoveCellAsync(Cell cell);

        Task RemoveGridAsync(Grid grid);

        Task SwapAsync<T>(T cell)
            where T : Cell;

        #endregion Public Methods
    }
}