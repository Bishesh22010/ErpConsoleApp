using System;
using System.Collections.Generic;
using Terminal.Gui;

namespace ErpConsoleApp.UI
{
    public static class UndoManager
    {
        // Stores the description, the database reverse-action, and an optional UI refresh action
        private static Stack<(string Description, Action UndoAction, Action RefreshAction)> undoStack = new Stack<(string, Action, Action)>();

        public static void Push(string description, Action undoAction, Action refreshAction = null)
        {
            undoStack.Push((description, undoAction, refreshAction));
        }

        public static void Undo()
        {
            if (undoStack.Count == 0)
            {
                MessageBox.Query("Undo", "There are no recent actions to undo.", "Ok");
                return;
            }

            var command = undoStack.Pop();

            if (MessageBox.Query("Confirm Undo", $"Are you sure you want to reverse this action?\n\nAction: {command.Description}", "Yes", "No") == 0)
            {
                try
                {
                    command.UndoAction.Invoke();
                    command.RefreshAction?.Invoke(); // Refresh the screen automatically
                    MessageBox.Query("Undo Successful", $"Successfully reversed:\n{command.Description}", "Ok");
                }
                catch (Exception ex)
                {
                    MessageBox.Query("Undo Failed", $"Failed to undo action:\n{ex.InnerException?.Message ?? ex.Message}", "Ok");
                    // Put the action back on the stack in case it was a temporary database lock
                    undoStack.Push(command);
                }
            }
            else
            {
                // User canceled the undo prompt, keep the action in memory
                undoStack.Push(command);
            }
        }
    }
}