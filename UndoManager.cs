using UnityEngine;
using System.Collections.Generic;

public interface IUndoCommand
{
    void Execute(); // やり直し・実行
    void Undo();    // 巻き戻し
}

public class UndoManager : MonoBehaviour
{
    public static UndoManager Instance { get; private set; }

    //命令を保存するスタック
    private Stack<IUndoCommand> undoStack = new Stack<IUndoCommand>();
    private Stack<IUndoCommand> redoStack = new Stack<IUndoCommand>();

    //最大保存数
    [SerializeField] private int maxUndoSteps = 50;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    ///<summary>
    ///すでに実行済みの操作をスタックに追加
    ///</summary>
    public void PushExistingCommand(IUndoCommand command)
    {
        undoStack.Push(command);
        redoStack.Clear();
    }

    /// <summary>
    /// 巻き戻し
    /// </summary>
    public void Undo()
    {
        if (undoStack.Count > 0)
        {
            IUndoCommand command = undoStack.Pop();
            command.Undo();
            redoStack.Push(command);
            Debug.Log("Undo Executed");
        }
        else
        {
            Debug.Log("Nothing to Undo");
        }
    }

    /// <summary>
    /// やり直し
    /// </summary>
    public void Redo()
    {
        if (redoStack.Count > 0)
        {
            IUndoCommand command = redoStack.Pop();
            command.Execute();
            undoStack.Push(command);
            Debug.Log("Redo Executed");
        }
        else
        {
            Debug.Log("Nothing to Redo");
        }
    }
}

