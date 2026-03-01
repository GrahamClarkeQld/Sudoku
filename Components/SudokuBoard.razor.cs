using Microsoft.JSInterop;
using Sudoku.Models;
using System;
using System.Runtime.CompilerServices;
using static System.Reflection.Metadata.BlobBuilder;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection;

namespace Sudoku.Components
{
    public partial class SudokuBoard
    {
        // locals --------------------------------------------------------------------

        private int[,] _values = new int[9, 9];
        private int _activeButton = -1;
        private ActionsList _actions = new();
        private string _currentAppVersion = "";

        private List<SudokuGrid> _grids = new List<SudokuGrid>();
        private SudokuGrid NewGrid { set => _grids.Add(value); }

        // cascading parameters -----------------------------------------------------------------------

        public List<SavedGame> SavedGames = new();
        public List<NumberedButton> NumberedButtons = new();
        public bool NumberEntryMode = true;
        public string CurrentTitle = "no name";


        // properties ---------------------------------------------------------------------------

        public int SelectedGrid { get; set; }
        public int SelectedCell { get; set; }

        // constructors

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            for (int idx = 0; idx < 9; idx++)
                NumberedButtons.Add(new NumberedButton(idx));
            SelectedCell = -1;
            SelectedGrid = -1;
            _currentAppVersion = GetAppVersion();
            await LoadSettings();
        }

        // setup

        public string GetAppVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Version? version = assembly.GetName().Version;
            return version.ToString();
        }

        private async Task LoadSettings()
        {
            List<string> settingsList = await SettingsService.LoadStringListAsync();
            foreach (string setting in settingsList)
            {
                string[] args = setting.Split(':');
                SavedGames.Add(new SavedGame(SavedGames.Count+1, args[0], args[1]));
            }
        }

        // events

        
        // callbacks ----------------------------------------------------------------------------------------------------------------

        private async Task ActionRequest((SudokuAction action, bool reverseAction) args)
        {
            SudokuAction action = (SudokuAction)args.Item1;
            bool reverseAction = (bool)args.Item2;

            SudokuAction actionToApply = new SudokuAction(action.GridIndex, action.CellIndex, action.NumberWasEntered,
                                                          (reverseAction) ? action.OldValue : action.NewValue,
                                                          (reverseAction) ? action.NewValue : action.OldValue);

            SetSelectedCell(action.GridIndex, action.CellIndex);
            if (action.NumberWasEntered)
            {
                if (actionToApply.OldValue != 0)
                    NumberedButtons[actionToApply.OldValue - 1].Usage--;
                await SetNumber(actionToApply);
            }
            else
            {
                ChildGrid(actionToApply.GridIndex).ToggleCandidate(actionToApply.CellIndex, action.NewValue + action.OldValue);
            }
        }

        private async Task ClearCellRequest()
        {
            if ((SelectedGrid == -1) || (SelectedCell == -1))
                return;

            if (ChildGrid(SelectedGrid).Value(SelectedCell) != 0)
            {
                NumberedButtons[_values[SelectedGrid, SelectedCell]].Usage--;
                await AddMove(new SudokuMove(SelectedGrid, SelectedCell, true, 0, _values[SelectedGrid, SelectedCell]));
            }
        }

        private async Task ClearSavedGamesRequest()
        {
            SavedGames.Clear();
            await SaveSettings();
        }

        private void GameModeChangeRequest()
        {
            NumberEntryMode = !NumberEntryMode;
        }

        private async Task LoadGameRequest(int gameId)
        {
            Console.WriteLine($"LoadGameRequest Id={gameId}");
            ResetGameRequest();
            foreach (SavedGame game in SavedGames)
            {
                Console.WriteLine($"SavedGame {game.Id}: {game.Title} = {game.GameString}");
                if (game.Id == gameId)
                {
                    CurrentTitle = game.Title;
                    Console.WriteLine($"Matched game: {game.Id} {game.Title} = {game.GameString} (len {game.GameString.Length})");
                    for (int idx = 0; idx < game.GameString.Length; idx += 3)
                    {
                        int gridIdx = (int)game.GameString[idx] - 49;
                        int cellIdx = (int)game.GameString[idx + 1] - 49;
                        int number = (int)game.GameString[idx + 2] - 48;
                        Console.WriteLine($"portion: [{gridIdx},{cellIdx}] = {number}");
                        await SetNumber(new SudokuAction(gridIdx,cellIdx, true, number, 0));
                    }
                }
            }
            _actions.SetInitialState();
        }

        private async Task NumberedButtonClickRequest(int btnId)
        {
            if (NumberEntryMode)
            {
                if ((SelectedGrid == -1) || (SelectedCell == -1))
                    return;
                if (_values[SelectedGrid, SelectedCell] != 0)
                    return;
                if (IsValidMove(SelectedGrid, SelectedCell, NumberedButtons[btnId].Number))
                {
                    SudokuMove move = new(SelectedGrid, SelectedCell, true, NumberedButtons[btnId].Number, 0);
                    RemoveMatchingCandidates(move);
                    await AddMove(move);
                }
            }
            else
            {
                _activeButton = btnId;
            }
        }

        private async Task SaveGameRequest(string title)
        {
            Console.WriteLine($"SaveGameRequest: {title}");
            if (CurrentTitle == title)
            { 
                bool okToOverwrite = await JsRuntime.InvokeAsync<bool>("confirm", $"Do you want to overwrite saved game '{title}'?");
                if (okToOverwrite)
                    RemoveCurrentSavedGame();
                else
                    return;
            }
            CurrentTitle = title;
            SavedGames.Add(new SavedGame(SavedGames.Count + 1, CurrentTitle, this.ToString()));
            await SaveSettings();
        }

        private void ResetGameRequest()
        {
            foreach (NumberedButton btn in NumberedButtons)
                btn.Reset();
            for (int gridIdx = 0; gridIdx < 9; gridIdx++)
                for (int cellIdx = 0; cellIdx < 9; cellIdx++)
                    _values[gridIdx, cellIdx] = 0;
            foreach (SudokuGrid grid in _grids)
                grid.Reset();
        }

        private void SetSelectedCellRequest((int gridArg, int cellArg) args)
        {
            SetSelectedCell(args.Item1, args.Item2);

            if ((!NumberEntryMode) && (_activeButton > -1)
            && (_values[SelectedGrid, SelectedCell] == 0)
            && IsValidMove(SelectedGrid, SelectedCell, NumberedButtons[_activeButton].Number))
            {
                ChildGrid(SelectedGrid).ToggleCandidate(SelectedCell, NumberedButtons[_activeButton].Number);
                SudokuMove move = new(SelectedGrid, SelectedCell, false, NumberedButtons[_activeButton].Number, 0);
                _actions.AddMove(move);
            }
        }


        // style related ----------------------------------------------------------------------------------------------------


        // Overrides ----------------------------------------------------------------------------

        public override string ToString()
        {
            string result = "";
            for (int gridIdx = 0; gridIdx < 9; gridIdx++)
                for (int cellIdx = 0; cellIdx < 9; cellIdx++)
                    if (_values[gridIdx, cellIdx] != 0)
                    {
                        result += $"{gridIdx + 1}{cellIdx + 1}{_values[gridIdx, cellIdx]}";
                        Console.WriteLine($"ToString={result}");
                    }
            return result;
        }

        // Methods ---------------------------------------------------------------------------------------

        private async Task AddMove(SudokuMove move)
        {
            await SetNumber(move.Actions[0]);
            _actions.AddMove(move);
        }

        private SudokuGrid ChildGrid(int gridArg)
        {
            return _grids.ElementAt(gridArg);
        }

        private bool IsValidMove(int gridArg, int cellArg, int number)
        {
            for (int cellIdx = 0; cellIdx < 9; cellIdx++)
                if (cellArg != cellIdx)
                    if (_values[gridArg, cellIdx] == number)
                        return false;

            int newCol = -1;
            int newRow = -1;
            OverallPosition(gridArg, cellArg, ref newRow, ref newCol);

            for (int gridIdx = 0; gridIdx < 9; gridIdx++)
                if (gridIdx != gridArg)
                    for (int cellIdx = 0; cellIdx < 9; cellIdx++)
                    {
                        int checkRow = -1;
                        int checkCol = -1;
                        OverallPosition(gridIdx, cellIdx, ref checkRow, ref checkCol);
                        if (((newRow == checkRow) || (newCol == checkCol))
                        && (_values[gridIdx, cellIdx] == number))
                            return false;
                    }
            return true;
        }

        private void OverallPosition(int gridArg, int cellArg, ref int rowPos, ref int colPos)
        {
            rowPos = (gridArg / 3) * 3 + (cellArg / 3);
            colPos = (gridArg % 3) * 3 + (cellArg % 3);
        }

        private void RemoveMatchingCandidates(SudokuMove move)
        {
            for (int cellIdx = 0; cellIdx < 9; cellIdx++)
                if ((move.Actions[0].CellIndex != cellIdx)
                && (_values[move.Actions[0].GridIndex, cellIdx] == 0)
                && (ChildGrid(move.Actions[0].GridIndex).IsCandidateSet(cellIdx, move.Actions[0].NewValue)))
                {
                    move.AddAction(new SudokuAction(move.Actions[0].GridIndex, cellIdx, false, move.Actions[0].NewValue, 0));
                    ChildGrid(move.Actions[0].GridIndex).ToggleCandidate(cellIdx, move.Actions[0].NewValue);
                }
            int newRow = -1;
            int newCol = -1;
            OverallPosition(SelectedGrid, SelectedCell, ref newRow, ref newCol);
            for (int gridIdx = 0; gridIdx < 9; gridIdx++)
                if (gridIdx != move.Actions[0].GridIndex)
                    for (int cellIdx = 0; cellIdx < 9; cellIdx++)
                        if (_values[gridIdx, cellIdx] == 0)
                        {
                            int checkRow = -1;
                            int checkCol = -1;
                            OverallPosition(gridIdx, cellIdx, ref checkRow, ref checkCol);
                            if (((newRow == checkRow) || (newCol == checkCol))
                            && ChildGrid(gridIdx).IsCandidateSet(cellIdx, move.Actions[0].NewValue))
                            {
                                ChildGrid(gridIdx).ToggleCandidate(cellIdx, move.Actions[0].NewValue);
                                move.AddAction(new SudokuAction(gridIdx, cellIdx, false, move.Actions[0].NewValue, 0));
                            }
                        }
        }

        private async Task SetNumber(SudokuAction action)
        {
            if (action.NewValue != 0)
                NumberedButtons[action.NewValue - 1].Usage++;
            _values[action.GridIndex, action.CellIndex] = action.NewValue;
            ChildGrid(action.GridIndex).SetValue(action.CellIndex, action.NewValue);

            foreach (NumberedButton btn in NumberedButtons)
                if (btn.Usage < 9)
                    return;
            await JsRuntime.InvokeVoidAsync("alert", "Congratulations! You have completed the game.");

            if (CurrentTitle != "")
                RemoveCurrentSavedGame();
        }

        private void RemoveCurrentSavedGame()
        {
            SavedGames.RemoveAll(SavedGame => SavedGame.Title == _currentTitle);
        }
        private async Task SaveSettings()
        {
            List<string> settingsList = new();
            foreach (SavedGame game in SavedGames)
                settingsList.Add(game.ToString());
            await SettingsService.SaveStringListAsync(settingsList);
        }


        private void SetSelectedCell(int gridArg, int cellArg)
        {
            if ((SelectedGrid != -1) && (SelectedCell != -1))
                ChildGrid(SelectedGrid).SetBorder(SelectedCell, false);

            SelectedGrid = gridArg;
            SelectedCell = cellArg;
            ChildGrid(SelectedGrid).SetBorder(SelectedCell, true);
        }


    } // SudokuBoard class
}
