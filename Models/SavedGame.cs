namespace Sudoku.Models
{
    public class SavedGame
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string GameString { get; set; }

        public SavedGame(int id, string title, string gameString)
        { 
            Id = id;
            Title = title;
            GameString = gameString;
        }

        public override string ToString()
        {
            return $"{Title}:{GameString}";
        }
    }
}
