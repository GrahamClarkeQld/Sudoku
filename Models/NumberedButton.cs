namespace Sudoku.Models
{
    public class NumberedButton
    {
        private int _usage = 0;

        public int Id { get; set; }

        public bool IsDisabled { get; set; } = false;

        public int Number { get; private set; }

        public int Usage
        {
            get { return _usage; }
            set
            {
                _usage = value;
                IsDisabled = (_usage == 9);
            }
        }

        public NumberedButton(int id)
        {
            Id = id;
            Number = id + 1;
        }

        public void Reset()
        {
            Usage = 0;
        }


    }
}
