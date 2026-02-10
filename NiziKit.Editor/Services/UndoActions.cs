using System.Numerics;
using System.Reflection;
using NiziKit.Core;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Services;

public class PropertyChangeAction : IMergeableAction
{
    private readonly object _instance;
    private readonly PropertyInfo _property;
    private readonly object? _oldValue;
    private object? _newValue;

    public PropertyChangeAction(object instance, PropertyInfo property, object? oldValue, object? newValue)
    {
        _instance = instance;
        _property = property;
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public string Description => $"Change {_property.Name}";

    public void Undo()
    {
        _property.SetValue(_instance, _oldValue);
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

public class TransformChangeAction : IMergeableAction
{
    private readonly GameObject _gameObject;
    private readonly Vector3 _oldPosition;
    private readonly Quaternion _oldRotation;
    private readonly Vector3 _oldScale;
    private Vector3 _newPosition;
    private Quaternion _newRotation;
    private Vector3 _newScale;

    public TransformChangeAction(GameObject gameObject,
        Vector3 oldPosition, Quaternion oldRotation, Vector3 oldScale,
        Vector3 newPosition, Quaternion newRotation, Vector3 newScale)
    {
        _gameObject = gameObject;
        _oldPosition = oldPosition;
        _oldRotation = oldRotation;
        _oldScale = oldScale;
        _newPosition = newPosition;
        _newRotation = newRotation;
        _newScale = newScale;
    }

    public string Description => "Transform Change";

    public void Undo()
    {
        _gameObject.LocalPosition = _oldPosition;
        _gameObject.LocalRotation = _oldRotation;
        _gameObject.LocalScale = _oldScale;
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

public class GizmoTransformAction : IUndoAction
{
    private readonly GameObject _gameObject;
    private readonly Vector3 _oldPosition;
    private readonly Quaternion _oldRotation;
    private readonly Vector3 _oldScale;
    private readonly Vector3 _newPosition;
    private readonly Quaternion _newRotation;
    private readonly Vector3 _newScale;

    public GizmoTransformAction(GameObject gameObject,
        Vector3 oldPosition, Quaternion oldRotation, Vector3 oldScale,
        Vector3 newPosition, Quaternion newRotation, Vector3 newScale)
    {
        _gameObject = gameObject;
        _oldPosition = oldPosition;
        _oldRotation = oldRotation;
        _oldScale = oldScale;
        _newPosition = newPosition;
        _newRotation = newRotation;
        _newScale = newScale;
    }

    public string Description => "Gizmo Transform";

    public void Undo()
    {
        _gameObject.LocalPosition = _oldPosition;
        _gameObject.LocalRotation = _oldRotation;
        _gameObject.LocalScale = _oldScale;
    }

    public void Redo()
    {
        _gameObject.LocalPosition = _newPosition;
        _gameObject.LocalRotation = _newRotation;
        _gameObject.LocalScale = _newScale;
    }
}

public class NameChangeAction : IMergeableAction
{
    private readonly GameObjectViewModel _viewModel;
    private readonly string _oldName;
    private string _newName;

    public NameChangeAction(GameObjectViewModel viewModel, string oldName, string newName)
    {
        _viewModel = viewModel;
        _oldName = oldName;
        _newName = newName;
    }

    public string Description => "Rename Object";

    public void Undo()
    {
        _viewModel.Name = _oldName;
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

public class ActiveToggleAction : IUndoAction
{
    private readonly GameObjectViewModel _viewModel;
    private readonly bool _oldActive;
    private readonly bool _newActive;

    public ActiveToggleAction(GameObjectViewModel viewModel, bool oldActive, bool newActive)
    {
        _viewModel = viewModel;
        _oldActive = oldActive;
        _newActive = newActive;
    }

    public string Description => "Toggle Active";

    public void Undo()
    {
        _viewModel.IsActive = _oldActive;
    }

    public void Redo()
    {
        _viewModel.IsActive = _newActive;
    }
}

public class CreateObjectAction : IUndoAction
{
    private readonly EditorViewModel _editorVm;
    private readonly GameObjectViewModel _objectVm;
    private readonly GameObjectViewModel? _parentVm;
    private readonly string _description;

    public CreateObjectAction(EditorViewModel editorVm, GameObjectViewModel objectVm,
        GameObjectViewModel? parentVm, string description)
    {
        _editorVm = editorVm;
        _objectVm = objectVm;
        _parentVm = parentVm;
        _description = description;
    }

    public string Description => _description;

    public void Undo()
    {
        var scene = World.CurrentScene;
        if (scene == null) return;

        if (_parentVm != null)
        {
            _parentVm.RemoveChild(_objectVm);
        }
        else
        {
            scene.Destroy(_objectVm.GameObject);
            _editorVm.RootObjects.Remove(_objectVm);
        }

        if (_editorVm.SelectedGameObject == _objectVm)
        {
            _editorVm.SelectObject(null);
        }
    }

    public void Redo()
    {
        var scene = World.CurrentScene;
        if (scene == null) return;

        if (_parentVm != null)
        {
            _parentVm.AddChild(_objectVm);
        }
        else
        {
            scene.Add(_objectVm.GameObject);
            _editorVm.RootObjects.Add(_objectVm);
        }

        _editorVm.SelectObject(_objectVm);
    }
}

public class DeleteObjectAction : IUndoAction
{
    private readonly EditorViewModel _editorVm;
    private readonly GameObjectViewModel _objectVm;
    private readonly GameObjectViewModel? _parentVm;
    private readonly int _index;

    public DeleteObjectAction(EditorViewModel editorVm, GameObjectViewModel objectVm,
        GameObjectViewModel? parentVm, int index)
    {
        _editorVm = editorVm;
        _objectVm = objectVm;
        _parentVm = parentVm;
        _index = index;
    }

    public string Description => "Delete Object";

    public void Undo()
    {
        var scene = World.CurrentScene;
        if (scene == null) return;

        if (_parentVm != null)
        {
            _parentVm.GameObject.AddChild(_objectVm.GameObject);
            _parentVm.Children.Insert(Math.Min(_index, _parentVm.Children.Count), _objectVm);
        }
        else
        {
            scene.Add(_objectVm.GameObject);
            _editorVm.RootObjects.Insert(Math.Min(_index, _editorVm.RootObjects.Count), _objectVm);
        }

        _editorVm.SelectObject(_objectVm);
    }

    public void Redo()
    {
        var scene = World.CurrentScene;
        if (scene == null) return;

        if (_parentVm != null)
        {
            _parentVm.RemoveChild(_objectVm);
        }
        else
        {
            scene.Destroy(_objectVm.GameObject);
            _editorVm.RootObjects.Remove(_objectVm);
        }

        if (_editorVm.SelectedGameObject == _objectVm)
        {
            _editorVm.SelectObject(null);
        }
    }
}
