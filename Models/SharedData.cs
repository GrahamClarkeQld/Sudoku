namespace Sudoku.Models
{
    public class SharedData
    {
        public enum ControlMode
        { 
            NumbersEntry = 0,
            SavedGameControl,
            UserHelp
        }

        public List<SavedGame> SavedGames = new();
        public List<NumberedButton> NumberedButtons = new();
        public bool NumberEntryMode = true;
        public ControlMode CurrentControlMode = ControlMode.NumbersEntry;
        public bool GameHasChanged = false;
        public bool SelectedCellIsEmpty = true;
        public string CurrentTitle = "";
        public string VersionNumber = "0.0.0.0";
        public int SelectedGrid = -1;
        public int SelectedCell = -1;
        public int SelectedSavedGameId = -1;
        public int ActiveButton = -1;
    }
}
