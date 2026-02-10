using System.Collections;
using System.Numerics;
using System.Reflection;
using NiziKit.Core;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Services;

public class PropertyChangeAction(object instance, PropertyInfo property, object? oldValue, object? newValue)
    : IMergeableAction
{
    private readonly object _instance = instance;
    private readonly PropertyInfo _property = property;
    private object? _newValue = newValue;

    public string Description => $"Change {_property.Name}";

    public void Undo()
    {
        _property.SetValue(_instance, oldValue);
    }

    public void Redo()
    {
        _property.SetValue(_instance, _newValue);
    }

    public bool MergeWith(IUndoAction newer)
    {
        if (newer is PropertyChangeAction other
            && other._instance == _instance
            && other._property == _property)
        {
            _newValue = other._newValue;
            return true;
        }
        return false;
    }
}

public class TransformChangeAction(
    GameObject gameObject,
    Vector3 oldPosition,
    Quaternion oldRotation,
    Vector3 oldScale,
    Vector3 newPosition,
    Quaternion newRotation,
    Vector3 newScale)
    : IMergeableAction
{
    private readonly GameObject _gameObject = gameObject;
    private Vector3 _newPosition = newPosition;
    private Quaternion _newRotation = newRotation;
    private Vector3 _newScale = newScale;

    public string Description => "Transform Change";

    public void Undo()
    {
        _gameObject.LocalPosition = oldPosition;
        _gameObject.LocalRotation = oldRotation;
        _gameObject.LocalScale = oldScale;
    }

    public void Redo()
    {
        _gameObject.LocalPosition = _newPosition;
        _gameObject.LocalRotation = _newRotation;
        _gameObject.LocalScale = _newScale;
    }

    public bool MergeWith(IUndoAction newer)
    {
        if (newer is TransformChangeAction other && other._gameObject == _gameObject)
        {
            _newPosition = other._newPosition;
            _newRotation = other._newRotation;
            _newScale = other._newScale;
            return true;
        }
        return false;
    }
}

public class GizmoTransformAction(
    GameObject gameObject,
    Vector3 oldPosition,
    Quaternion oldRotation,
    Vector3 oldScale,
    Vector3 newPosition,
    Quaternion newRotation,
    Vector3 newScale)
    : IUndoAction
{
    public string Description => "Gizmo Transform";

    public void Undo()
    {
        gameObject.LocalPosition = oldPosition;
        gameObject.LocalRotation = oldRotation;
        gameObject.LocalScale = oldScale;
    }

    public void Redo()
    {
        gameObject.LocalPosition = newPosition;
        gameObject.LocalRotation = newRotation;
        gameObject.LocalScale = newScale;
    }
}

public class NameChangeAction(GameObjectViewModel viewModel, string oldName, string newName)
    : IMergeableAction
{
    private readonly GameObjectViewModel _viewModel = viewModel;
    private string _newName = newName;

    public string Description => "Rename Object";

    public void Undo()
    {
        _viewModel.Name = oldName;
    }

    public void Redo()
    {
        _viewModel.Name = _newName;
    }

    public bool MergeWith(IUndoAction newer)
    {
        if (newer is NameChangeAction other && other._viewModel == _viewModel)
        {
            _newName = other._newName;
            return true;
        }
        return false;
    }
}

public class ActiveToggleAction(GameObjectViewModel viewModel, bool oldActive, bool newActive)
    : IUndoAction
{
    public string Description => "Toggle Active";

    public void Undo()
    {
        viewModel.IsActive = oldActive;
    }

    public void Redo()
    {
        viewModel.IsActive = newActive;
    }
}

public class CreateObjectAction(
    EditorViewModel editorVm,
    GameObjectViewModel objectVm,
    GameObjectViewModel? parentVm,
    string description)
    : IUndoAction
{
    public string Description => description;

    public void Undo()
    {
        var scene = World.CurrentScene;
        if (scene == null)
        {
            return;
        }

        if (parentVm != null)
        {
            parentVm.RemoveChild(objectVm);
        }
        else
        {
            scene.Destroy(objectVm.GameObject);
            editorVm.RootObjects.Remove(objectVm);
        }

        if (editorVm.SelectedGameObject == objectVm)
        {
            editorVm.SelectObject(null);
        }
    }

    public void Redo()
    {
        var scene = World.CurrentScene;
        if (scene == null)
        {
            return;
        }

        if (parentVm != null)
        {
            parentVm.AddChild(objectVm);
        }
        else
        {
            scene.Add(objectVm.GameObject);
            editorVm.RootObjects.Add(objectVm);
        }

        editorVm.SelectObject(objectVm);
    }
}

public class DeleteObjectAction(
    EditorViewModel editorVm,
    GameObjectViewModel objectVm,
    GameObjectViewModel? parentVm,
    int index)
    : IUndoAction
{
    public string Description => "Delete Object";

    public void Undo()
    {
        var scene = World.CurrentScene;
        if (scene == null)
        {
            return;
        }

        if (parentVm != null)
        {
            parentVm.GameObject.AddChild(objectVm.GameObject);
            parentVm.Children.Insert(Math.Min(index, parentVm.Children.Count), objectVm);
        }
        else
        {
            scene.Add(objectVm.GameObject);
            editorVm.RootObjects.Insert(Math.Min(index, editorVm.RootObjects.Count), objectVm);
        }

        editorVm.SelectObject(objectVm);
    }

    public void Redo()
    {
        var scene = World.CurrentScene;
        if (scene == null)
        {
            return;
        }

        if (parentVm != null)
        {
            parentVm.RemoveChild(objectVm);
        }
        else
        {
            scene.Destroy(objectVm.GameObject);
            editorVm.RootObjects.Remove(objectVm);
        }

        if (editorVm.SelectedGameObject == objectVm)
        {
            editorVm.SelectObject(null);
        }
    }
}

public class ListChangeAction(IList list, object[] oldSnapshot, object[] newSnapshot, string description)
    : IUndoAction
{
    public string Description { get; } = description;

    public void Undo()
    {
        list.Clear();
        foreach (var entry in oldSnapshot)
        {
            list.Add(entry);
        }
    }

    public void Redo()
    {
        list.Clear();
        foreach (var entry in newSnapshot)
        {
            list.Add(entry);
        }
    }
}

public class DictionaryChangeAction(
    IDictionary dict,
    KeyValuePair<object, object>[] oldSnapshot,
    KeyValuePair<object, object>[] newSnapshot,
    string description)
    : IUndoAction
{
    public string Description { get; } = description;

    public void Undo()
    {
        dict.Clear();
        foreach (var entry in oldSnapshot)
        {
            dict[entry.Key] = entry.Value;
        }
    }

    public void Redo()
    {
        dict.Clear();
        foreach (var entry in newSnapshot)
        {
            dict[entry.Key] = entry.Value;
        }
    }
}
