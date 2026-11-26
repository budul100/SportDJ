using System.Threading.Tasks;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Commons.Interfaces;

public interface IGridService
{
    #region Public Properties

    Grid Grid { get; }

    #endregion Public Properties

    #region Public Methods

    void AddGrid(Grid grid, OrientationType orientation);

    void AddPlayer(Cell cell, OrientationType orientation);

    void CancelListening();

    void ClearMidi(Grid grid);

    Task GroupAsync(Player player);

    bool IsListening(Grid grid);

    bool IsListeningMidi(Grid grid);

    void ListenKey(Grid grid);

    void ListenMidi(Grid grid);

    void MoveCell(Cell cell, OrientationType orientation);

    void MoveGrid(Grid grid, OrientationType orientation);

    Task RemoveCellAsync(Cell cell);

    Task RemoveGridAsync(Grid grid);

    void SetKeyCode(Grid grid, string keyCode);

    void SetName(Grid grid, string name);

    #endregion Public Methods
}
