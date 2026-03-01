using System;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Sudoku.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sudoku.Components
{
    public partial class ActionsList
    {
        // locals

        private Stack<SudokuMove> _undoStack = new Stack<SudokuMove>();
        private Stack<SudokuMove> _redoStack = new Stack<SudokuMove>();
        private List<string> _breakpoints = new();
        private int _selectedSavedGameId = -1;

        // properties

        [CascadingParameter(Name = "Common Data")]
        private SharedData CommonData { get; set; }

        // constructors

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        // user settings

        // events

        private void AddBreakpointButtonClick()
        {
            SudokuMove move = _undoStack.Peek();
            if (!move.IsBreakpoint)
                SetUndoBreakpoint(true);
        }

        private async Task ClearButtonClick()
        {
            await ClearCellRequest.InvokeAsync();
        }

        private async Task ClearSavedGamesButtonClick()
        {
            await ClearSavedGamesRequest.InvokeAsync();
        }

        private async Task HandleGameSelection()
        { 
            await LoadGameRequest.InvokeAsync(_selectedSavedGameId);
        }

        private async Task RedoButtonClick()
        {

            SudokuMove move = _redoStack.Pop();
            _undoStack.Push(move);
            if (move.IsBreakpoint)
                AddBreakpoint(move);

            foreach (SudokuAction action in move.Actions)
                await ActionRequest.InvokeAsync((action, false));

        }

        private async Task ResetButtonClick()
        {
            bool confirmReset = await JsRuntime.InvokeAsync<bool>("confirm", "Really Reset the game?");
            if (confirmReset)
                await ResetGameRequest.InvokeAsync();
        }

        private async Task ReturnToBreakpointButtonClick()
        {
            while (_undoStack.Count > 0)
            {
                SudokuMove move = _undoStack.Peek();
                if (move.IsBreakpoint)
                {
                    SetUndoBreakpoint(false);
                    return;
                }
                await ProcessUndo();
            }
        }

        private async Task SaveGameButtonClick()
        {
            string title = await JsRuntime.InvokeAsync<string>("prompt", "Please enter the game's Title:", CommonData.CurrentTitle);
            if (title != "")
            {
                await SaveGameRequest.InvokeAsync(title);
            }
        }

        private async Task UndoButtonClick()
        {
            await ProcessUndo();
        }

        // callbacks

        [Parameter]
        public EventCallback<(SudokuAction, bool)> ActionRequest { get; set; }

        [Parameter]
        public EventCallback ClearCellRequest { get; set; }

        [Parameter]
        public EventCallback ClearSavedGamesRequest { get; set; }

        [Parameter]
        public EventCallback<int> LoadGameRequest { get; set; }

        [Parameter]
        public EventCallback ResetGameRequest { get; set; }

        [Parameter]
        public EventCallback<string> SaveGameRequest { get; set; }

        // style

        // methods

        private void AddBreakpoint(SudokuMove move)
        {
            _breakpoints.Add($"{move.ToString()}");
        }

        public void AddMove(SudokuMove move)
        {
            _undoStack.Push(move);
            SudokuMove redoMove;

            while (_redoStack.Count > 0)
            {
                redoMove = _redoStack.Pop();
                if (redoMove.IsBreakpoint)
                    RemoveBreakpoint();
            }
        }

        private bool CurrentUndoHasBreakpoint()
        {
            if (_undoStack.Count == 0) return false;
            SudokuMove move = _undoStack.Peek();
            return move.IsBreakpoint;
        }

        private async Task ProcessUndo()
        {
            SudokuMove move = _undoStack.Pop();
            _redoStack.Push(move);
            if (move.IsBreakpoint)
                RemoveBreakpoint();
            foreach (SudokuAction action in move.Actions)
            {
                await ActionRequest.InvokeAsync((action, true));
            }
        }

        private void RemoveBreakpoint()
        {
            _breakpoints.RemoveAt(_breakpoints.Count - 1);
        }

        public void SetInitialState()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _breakpoints.Clear();
        }

        private void SetUndoBreakpoint(bool setOn)
        {
            SudokuMove move = _undoStack.Pop();
            move.IsBreakpoint = setOn;
            _undoStack.Push(move);
            if (setOn)
                AddBreakpoint(move);
        }

        public async Task Reset()
        {
            while (_undoStack.Count > 0)
            {
                await ProcessUndo();
            }
        }

        private bool UnableToAddBreakpoint()
        {
            return (_undoStack.Count == 0) || CurrentUndoHasBreakpoint();
        }

        private bool UnableToReturnToBreakpoint()
        {
            return (_undoStack.Count == 0) || CurrentUndoHasBreakpoint() || (_breakpoints.Count == 0);
        }

    } // ActionList class
}
