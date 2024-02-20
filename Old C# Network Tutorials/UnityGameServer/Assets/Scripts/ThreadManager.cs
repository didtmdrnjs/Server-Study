using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ThreadManager : MonoBehaviour
{
    private static readonly List<Action> executeOnMainThread = new List<Action>();
    private static readonly List<Action> executeCopiedOnMainThread = new List<Action>();
    private static bool actionToExecuteOnMainThread = false;

    private void FixedUpdate()
    {
        UpdateMain();
    }

    /// <summary>Sets an action to be executed on the main thread.</summary>
    /// <param name="_action">The action to be executed on the main thread.</param>
    public static void ExecuteOnMainThread(Action _action)
    {
        if (_action == null)
        {
            Console.WriteLine("No action to execute on main thread!");
            return;
        }

        // ���� ������ ����
        lock (executeOnMainThread)
        {
            // Action �߰�
            executeOnMainThread.Add(_action);
            actionToExecuteOnMainThread = true;
        }
    }

    /// <summary>Executes all code meant to run on the main thread. NOTE: Call this ONLY from the main thread.</summary>
    public static void UpdateMain()
    {
        if (actionToExecuteOnMainThread)
        {
            // Copied List ����, ���� ������ List ���� ����
            executeCopiedOnMainThread.Clear();
            lock (executeOnMainThread)
            {
                // ���� ������ List ���� ����
                executeCopiedOnMainThread.AddRange(executeOnMainThread);
                // ���� ������ List ����
                executeOnMainThread.Clear();
                actionToExecuteOnMainThread = false;
            }

            // Copied List�� Action�� ����
            for (int i = 0; i < executeCopiedOnMainThread.Count; i++)
            {
                executeCopiedOnMainThread[i]();
            }
        }
    }
}
