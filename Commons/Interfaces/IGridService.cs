using System.Threading.Tasks;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Commons.Interfaces
{
    public interface IGridService
    {
        #region Public Properties

        Grid Grid { get; }

        #endregion Public Properties

        #region Public Methods

        void Add<T>(Cell cell, OrientationType orientation)
            where T : Cell;

        void Move(Cell cell, OrientationType orientation);

        Task RemoveAsync(Cell cell);

        Task SwapAsync<T>(T cell)
            where T : Cell;

        #endregion Public Methods
    }
}