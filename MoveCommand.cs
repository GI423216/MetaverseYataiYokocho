using System.Collections.Generic;
using UnityEngine;
// 移動用
public class MoveCommand : IUndoCommand
{
    private Dictionary<Transform, (Vector3 oldPos, Vector3 newPos)> _records;
    public MoveCommand(Dictionary<Transform, (Vector3 oldPos, Vector3 newPos)> records) => _records = records;

    public void Execute() { foreach (var r in _records) if (r.Key) r.Key.localPosition = r.Value.newPos; }
    public void Undo() { foreach (var r in _records) if (r.Key) r.Key.localPosition = r.Value.oldPos; }
}

// 回転用
public class RotateCommand : IUndoCommand
{
    private Dictionary<Transform, (Quaternion oldRot, Quaternion newRot)> _records;
    public RotateCommand(Dictionary<Transform, (Quaternion oldRot, Quaternion newRot)> records) => _records = records;

    public void Execute() { foreach (var r in _records) if (r.Key) r.Key.localRotation = r.Value.newRot; }
    public void Undo() { foreach (var r in _records) if (r.Key) r.Key.localRotation = r.Value.oldRot; }
}

// スケール用
public class ScaleCommand : IUndoCommand
{
    private Dictionary<Transform, (Vector3 oldSca, Vector3 newSca)> _records;
    public ScaleCommand(Dictionary<Transform, (Vector3 oldSca, Vector3 newSca)> records) => _records = records;

    public void Execute() { foreach (var r in _records) if (r.Key) r.Key.localScale = r.Value.newSca; }
    public void Undo() { foreach (var r in _records) if (r.Key) r.Key.localScale = r.Value.oldSca; }
}

public class CreateCommand : IUndoCommand
{
    private List<GameObject> _createdObjects;
    private List<GameObject> _globalList; // MainManagerなどのYataiObjectsへの参照

    public CreateCommand(List<GameObject> objects, List<GameObject> globalList)
    {
        _createdObjects = new List<GameObject>(objects);
        _globalList = globalList;
    }

    public void Execute()
    {
        foreach (var obj in _createdObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                if (!_globalList.Contains(obj)) _globalList.Add(obj);
            }
        }
    }

    public void Undo()
    {
        foreach (var obj in _createdObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                _globalList.Remove(obj);
            }
        }
    }
}

public class DeleteCommand : IUndoCommand
{
    private List<GameObject> _deletedObjects;
    private List<GameObject> _globalList;

    public DeleteCommand(List<GameObject> objects, List<GameObject> globalList)
    {
        _deletedObjects = new List<GameObject>(objects);
        _globalList = globalList;
    }

    //やり直し
    public void Execute()
    {
        foreach (var obj in _deletedObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                _globalList.Remove(obj);
            }
        }
    }

    //巻き戻し
    public void Undo()
    {
        foreach (var obj in _deletedObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                if (!_globalList.Contains(obj)) _globalList.Add(obj);
            }
        }
    }
}