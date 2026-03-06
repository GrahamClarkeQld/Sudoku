namespace Sudoku.Models
{
    public class SharedData
    {
        public List<SavedGame> SavedGames = new();
        public List<NumberedButton> NumberedButtons = new();
        public bool NumberEntryMode = true;
        public bool NumbersAreActive = true;
        public string CurrentTitle = "no name";
        public string VersionNumber = "0.0.0.0";
        public int SelectedGrid = -1;
        public int SelectedCell = -1;
    }
}
