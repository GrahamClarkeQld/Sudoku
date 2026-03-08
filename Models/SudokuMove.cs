namespace Sudoku.Models
{
    public class SudokuMove
    {
        public List<SudokuAction> Actions = new();
        public int Number { get; private set; }

        public bool IsBreakpoint { get; set; }

        public SudokuMove(int gridArg, int cellArg, bool numberWasEntered, int newValue, int oldValue)
        {
            Actions.Add(new SudokuAction(gridArg, cellArg, numberWasEntered, newValue, oldValue));
            IsBreakpoint = false;
        }

        public SudokuMove()
        { 
            IsBreakpoint = false;
        }

        public void AddAction(SudokuAction action)
        { Actions.Add(action); }

        public void Reset()
        { Actions.Clear(); }

        public override string ToString()
        {
            return Actions[0].ToString();
        }
    }

    public class SudokuAction
    {
        public int GridIndex { get; set; }
        public int CellIndex { get; set; }

        public bool NumberWasEntered { get; set; }

        public int NewValue { get; set; }

        public int OldValue { get; set; }

        public SudokuAction(int gridArg, int cellArg, bool numberWasEntered, int newValue, int oldValue)
        {
            GridIndex = gridArg;
            CellIndex = cellArg;
            NumberWasEntered = numberWasEntered;
            NewValue = newValue;
            OldValue = oldValue;
        }

        public void Switch()
        {
            int temp = OldValue;
            OldValue = NewValue;
            NewValue = temp;
        }

        public override string ToString()
        {
            return $"[{GridIndex + 1}, {CellIndex + 1}] = {NewValue}";
        }

    }
}
