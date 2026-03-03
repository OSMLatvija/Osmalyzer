using System;
using Osmalyzer.Commands;

namespace Osmalyzer;

/// <summary>
/// Tracks executed commands as undo/redo stacks, allowing commands to be undone and redone
/// </summary>
public class History
{
    /// <summary>Whether there is at least one command that can be undone</summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>Whether there is at least one command that can be redone</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>Number of commands currently on the undo stack</summary>
    public int UndoCount => _undoStack.Count;

    /// <summary>Number of commands currently on the redo stack</summary>
    public int RedoCount => _redoStack.Count;


    private readonly Stack<Command> _undoStack = new Stack<Command>();
    private readonly Stack<Command> _redoStack = new Stack<Command>();


    /// <summary> Records a new undo command, clearing the redo stack since the history has branched </summary>
    internal void RecordUndo(Command? undo)
    {
        if (undo == null) return;

        _undoStack.Push(undo);
        _redoStack.Clear();
    }

    /// <summary>
    /// Pops the top undo command, applies it (which yields a redo command), and pushes that onto the redo stack.
    /// Returns the applied undo command.
    /// </summary>
    internal Command Undo()
    {
        if (!CanUndo) throw new InvalidOperationException("Nothing to undo.");

        Command undoCommand = _undoStack.Pop();
        Command? redoCommand = undoCommand.Apply();

        if (redoCommand != null)
            _redoStack.Push(redoCommand);

        return undoCommand;
    }

    /// <summary>
    /// Pops the top redo command, applies it (which yields an undo command), and pushes that onto the undo stack.
    /// Returns the applied redo command.
    /// </summary>
    internal Command Redo()
    {
        if (!CanRedo) throw new InvalidOperationException("Nothing to redo.");

        Command redoCommand = _redoStack.Pop();
        Command? undoCommand = redoCommand.Apply();

        if (undoCommand != null)
            _undoStack.Push(undoCommand);

        return redoCommand;
    }
}