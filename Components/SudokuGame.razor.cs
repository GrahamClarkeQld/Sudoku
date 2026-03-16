using Microsoft.JSInterop;
using Sudoku.Models;
using System;
using System.Runtime.CompilerServices;
using static System.Reflection.Metadata.BlobBuilder;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection;
using System.Threading.Tasks;

namespace Sudoku.Components
{
    public partial class SudokuGame
    {
        // locals --------------------------------------------------------------------

        private int[,] _values = new int[9, 9];
        private ActionsList _actions = new();

        private List<SudokuGrid> _grids = new List<SudokuGrid>();
        private SudokuGrid NewGrid { set => _grids.Add(value); }

        // cascading parameters -----------------------------------------------------------------------

        public SharedData CommonData = new();

        // properties ---------------------------------------------------------------------------

        // constructors

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            for (int idx = 0; idx < 9; idx++)
                CommonData.NumberedButtons.Add(new NumberedButton(idx));
            CommonData.SelectedCell = -1;
            CommonData.SelectedGrid = -1;
            CommonData.VersionNumber = GetAppVersion();
            await LoadSettings();
        }

        // setup

        public string GetAppVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Version? version = assembly.GetName().Version;
            return $"{assembly.GetCustomAttributes(false).OfType<AssemblyTitleAttribute>().FirstOrDefault().Title}";
        }

        private async Task LoadSettings()
        {
            List<string> settingsList = await SettingsService.LoadStringListAsync();
            foreach (string setting in settingsList)
            {
                string[] args = setting.Split(':');
                CommonData.SavedGames.Add(new SavedGame(CommonData.SavedGames.Count+1, args[0], args[1]));
            }
        }

        // events

        private void ControlPanelButtonClick()
        {
            switch (CommonData.CurrentControlMode)
            {
                case SharedData.ControlMode.NumbersEntry:
                    CommonData.CurrentControlMode = SharedData.ControlMode.SavedGameControl;
                    break;
                case SharedData.ControlMode.SavedGameControl:
                    CommonData.CurrentControlMode = SharedData.ControlMode.NumbersEntry;
                    break;
                case SharedData.ControlMode.UserHelp:
                    CommonData.CurrentControlMode = SharedData.ControlMode.NumbersEntry;
                    break;
            }
        }
        
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
                    CommonData.NumberedButtons[actionToApply.OldValue - 1].Usage--;
                await SetNumber(actionToApply);
            }
            else
            {
                ChildGrid(actionToApply.GridIndex).ToggleCandidate(actionToApply.CellIndex, action.NewValue + action.OldValue);
            }
        }

        private async Task ClearCellRequest()
        {
            if ((CommonData.SelectedGrid == -1) || (CommonData.SelectedCell == -1))
                return;

            if (ChildGrid(CommonData.SelectedGrid).Value(CommonData.SelectedCell) == 0)
            {
                SudokuMove move = new();
                bool anyCandidates = false;
                for (int cellIdx = 0; cellIdx < 9; cellIdx++)
                    if (ChildGrid(CommonData.SelectedGrid).IsCandidateSet(CommonData.SelectedCell, cellIdx + 1))
                    {
                        if (!anyCandidates)
                            anyCandidates = true;
                        ChildGrid(CommonData.SelectedGrid).ToggleCandidate(CommonData.SelectedCell, cellIdx + 1);
                        move.AddAction(new SudokuAction(CommonData.SelectedGrid, CommonData.SelectedCell, false, 0, cellIdx + 1));
                    }
                if (anyCandidates)
                    await AddMove(move);
            }
            else
            {
                CommonData.NumberedButtons[_values[CommonData.SelectedGrid, CommonData.SelectedCell]-1].Usage--;
                await AddMove(new SudokuMove(CommonData.SelectedGrid, CommonData.SelectedCell, true, 0, _values[CommonData.SelectedGrid, CommonData.SelectedCell]));
            }
        }

        private async Task ClearSavedGamesRequest()
        {
            if (await JsRuntime.InvokeAsync<bool>("confirm", "Really clear all the saved games?"))
            {
                CommonData.SavedGames.Clear();
                await SaveSettings();
            }
        }

        private void GameModeChangeRequest()
        {
            CommonData.NumberEntryMode = !CommonData.NumberEntryMode;
        }

        private void InfoButtonClick()
        {
            if (CommonData.CurrentControlMode != SharedData.ControlMode.UserHelp)
                CommonData.CurrentControlMode = SharedData.ControlMode.UserHelp;
        }

        private async Task LoadGameRequest(int gameId)
        {
            Console.WriteLine($"LoadGameRequest Id={gameId}");
            ResetGameRequest();
            foreach (SavedGame game in CommonData.SavedGames)
            {
                Console.WriteLine($"SavedGame {game.Id}: {game.Title} = {game.GameString}");
                if (game.Id == gameId)
                {
                    CommonData.CurrentTitle = game.Title;
                    Console.WriteLine($"Matched game: {game.Id} {game.Title} = {game.GameString} (len {game.GameString.Length})");
                    for (int idx = 0; idx < game.GameString.Length; idx += 3)
                    {
                        int gridIdx = (int)game.GameString[idx] - 49;
                        int cellIdx = (int)game.GameString[idx + 1] - 49;
                        int number = (int)game.GameString[idx + 2] - 48;
                        SetSelectedCell(gridIdx, cellIdx);
                        await SetNumber(new SudokuAction(gridIdx,cellIdx, true, number, 0));
                    }
                }
            }
            _actions.SetInitialState();

            CommonData.GameHasChanged = false;
        }

        private async Task NumberedButtonClickRequest(int btnId)
        {
            if (CommonData.NumberEntryMode)
            {
                if ((CommonData.SelectedGrid == -1) || (CommonData.SelectedCell == -1))
                    return;
                if (_values[CommonData.SelectedGrid, CommonData.SelectedCell] != 0)
                    return;
                if (await IsValidMove(CommonData.SelectedGrid, CommonData.SelectedCell, CommonData.NumberedButtons[btnId].Number))
                {
                    SudokuMove move = new(CommonData.SelectedGrid, CommonData.SelectedCell, true, CommonData.NumberedButtons[btnId].Number, 0);
                    RemoveMatchingCandidates(move);
                    await AddMove(move);
                }
            }
            else
            {
                CommonData.ActiveButton = btnId;
            }
        }

        private async Task SaveGameRequest(string title)
        {
            Console.WriteLine($"SaveGameRequest: {title}");
            if (CommonData.CurrentTitle == title)
            { 
                bool okToOverwrite = await JsRuntime.InvokeAsync<bool>("confirm", $"Do you want to overwrite saved game '{title}'?");
                if (okToOverwrite)
                    await RemoveCurrentSavedGame();
                else
                    return;
            }
            CommonData.CurrentTitle = title;
            CommonData.SavedGames.Add(new SavedGame(CommonData.SavedGames.Count + 1, CommonData.CurrentTitle, this.ToString()));
            await SaveSettings();
        }

        private void ResetGameRequest()
        {
            foreach (NumberedButton btn in CommonData.NumberedButtons)
                btn.Reset();
            for (int gridIdx = 0; gridIdx < 9; gridIdx++)
                for (int cellIdx = 0; cellIdx < 9; cellIdx++)
                    _values[gridIdx, cellIdx] = 0;
            foreach (SudokuGrid grid in _grids)
                grid.Reset();

            CommonData.GameHasChanged = false;
        }

        private async Task SetSelectedCellRequest((int gridArg, int cellArg) args)
        {
            SetSelectedCell(args.Item1, args.Item2);

            if ((!CommonData.NumberEntryMode) && (CommonData.ActiveButton > -1)
            && (_values[CommonData.SelectedGrid, CommonData.SelectedCell] == 0)
            && await IsValidMove(CommonData.SelectedGrid, CommonData.SelectedCell, CommonData.NumberedButtons[CommonData.ActiveButton].Number))
            {
                ChildGrid(CommonData.SelectedGrid).ToggleCandidate(CommonData.SelectedCell, CommonData.NumberedButtons[CommonData.ActiveButton].Number);
                CommonData.SelectedCellIsEmpty = ChildGrid(CommonData.SelectedGrid).IsEmpty(CommonData.SelectedCell);
                SudokuMove move = new(CommonData.SelectedGrid, CommonData.SelectedCell, false, CommonData.NumberedButtons[CommonData.ActiveButton].Number, 0);
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

        private async Task<bool> IsValidMove(int gridArg, int cellArg, int number)
        {
            for (int cellIdx = 0; cellIdx < 9; cellIdx++)
                if (cellArg != cellIdx)
                    if (_values[gridArg, cellIdx] == number)
                    {
                        await ShowError(gridArg, cellArg, gridArg, cellIdx);
                        return false;
                    }
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
                        {
                            await ShowError(gridArg, cellArg, gridIdx, cellIdx);
                            return false;
                        }
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
            OverallPosition(CommonData.SelectedGrid, CommonData.SelectedCell, ref newRow, ref newCol);
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
                CommonData.NumberedButtons[action.NewValue - 1].Usage++;
            _values[action.GridIndex, action.CellIndex] = action.NewValue;
            ChildGrid(action.GridIndex).SetValue(action.CellIndex, action.NewValue);
            CommonData.SelectedCellIsEmpty = ChildGrid(CommonData.SelectedGrid).IsEmpty(CommonData.SelectedCell);
            CommonData.GameHasChanged = true;
            foreach (NumberedButton btn in CommonData.NumberedButtons)
                if (btn.Usage < 9)
                    return;

            StateHasChanged();

            await JsRuntime.InvokeVoidAsync("alert", "Congratulations! You have completed the game.");

            if (CommonData.CurrentTitle != "")
                await RemoveCurrentSavedGame();
        }

        private async Task RemoveCurrentSavedGame()
        {
            CommonData.SavedGames.RemoveAll(SavedGame => SavedGame.Title == CommonData.CurrentTitle);
            await SaveSettings();
        }

        private async Task SaveSettings()
        {
            List<string> settingsList = new();
            foreach (SavedGame game in CommonData.SavedGames)
                settingsList.Add(game.ToString());
            await SettingsService.SaveStringListAsync(settingsList);
        }


        private void SetSelectedCell(int gridArg, int cellArg)
        {
            CommonData.SelectedGrid = gridArg;
            CommonData.SelectedCell = cellArg;
            CommonData.SelectedCellIsEmpty = ChildGrid(CommonData.SelectedGrid).IsEmpty(CommonData.SelectedCell);
        }


        private async Task ShowError(int candidateGrid, int candidateCell, int numberedGrid, int numberedCell)
        {
            ChildGrid(candidateGrid).ErrorState(candidateCell,true);
            ChildGrid(numberedGrid).ErrorState(numberedCell, true);
            await Task.Delay(500);
            ChildGrid(numberedGrid).ErrorState(numberedCell, false);
            ChildGrid(candidateGrid).ErrorState(candidateCell, false);
        }
    } // SudokuBoard class
}
