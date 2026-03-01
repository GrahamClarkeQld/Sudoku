namespace Sudoku.Models
{
    public class SharedData
    {
        public List<SavedGame> SavedGames = new();
        public List<NumberedButton> NumberedButtons = new();
        public bool NumberEntryMode = true;
        public string CurrentTitle = "no name";
    }
}
