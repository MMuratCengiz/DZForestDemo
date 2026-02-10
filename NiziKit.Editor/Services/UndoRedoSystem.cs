namespace NiziKit.Editor.Services;

public interface IUndoAction
{
    string Description { get; }
    void Undo();
    void Redo();
}

public interface IMergeableAction : IUndoAction
{
    bool MergeWith(IUndoAction newer);
}

public class UndoRedoSystem
{
    private const int MaxEntries = 256;
    private const float MergeTimeWindow = 0.4f;

    private readonly List<IUndoAction> _undoStack = [];
    private readonly List<IUndoAction> _redoStack = [];

    private string? _lastMergeKey;
    private DateTime _lastActionTime;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public string? UndoDescription => CanUndo ? _undoStack[^1].Description : null;
    public string? RedoDescription => CanRedo ? _redoStack[^1].Description : null;

    public void Execute(IUndoAction action, string? mergeKey = null)
    {
        var now = DateTime.UtcNow;

        if (mergeKey != null
            && mergeKey == _lastMergeKey
            && (now - _lastActionTime).TotalSeconds < MergeTimeWindow
            && _undoStack.Count > 0
            && _undoStack[^1] is IMergeableAction mergeable)
        {
            if (mergeable.MergeWith(action))
            {
                _lastActionTime = now;
                _redoStack.Clear();
                return;
            }
        }

        _undoStack.Add(action);
        _redoStack.Clear();

        if (_undoStack.Count > MaxEntries)
        {
            _undoStack.RemoveAt(0);
        }

        _lastMergeKey = mergeKey;
        _lastActionTime = now;
    }

    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        var action = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        action.Undo();
        _redoStack.Add(action);

        _lastMergeKey = null;
    }

    public void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        var action = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        action.Redo();
        _undoStack.Add(action);

        _lastMergeKey = null;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _lastMergeKey = null;
    }
}
