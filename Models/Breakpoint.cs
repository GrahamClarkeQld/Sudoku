namespace Sudoku.Models
{
    public class Breakpoint
    {

        private SudokuMove _move;

        public int SelectedGrid { get; set; }
        public int SelectedCell { get; set; }    
        public Breakpoint(int grid, int cell, SudokuMove move)
        {
            SelectedGrid = grid;
            SelectedCell = cell;
            _move = move;
        }

        public override string ToString()
        {
            return _move.ToString();
        }
    }
}
