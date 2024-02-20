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

        // 다중 스레드 방지
        lock (executeOnMainThread)
        {
            // Action 추가
            executeOnMainThread.Add(_action);
            actionToExecuteOnMainThread = true;
        }
    }

    /// <summary>Executes all code meant to run on the main thread. NOTE: Call this ONLY from the main thread.</summary>
    public static void UpdateMain()
    {
        if (actionToExecuteOnMainThread)
        {
            // Copied List 비우고, 메인 스레드 List 내용 복사
            executeCopiedOnMainThread.Clear();
            lock (executeOnMainThread)
            {
                // 메인 스레드 List 내용 복사
                executeCopiedOnMainThread.AddRange(executeOnMainThread);
                // 메인 스레드 List 비우기
                executeOnMainThread.Clear();
                actionToExecuteOnMainThread = false;
            }

            // Copied List의 Action들 실행
            for (int i = 0; i < executeCopiedOnMainThread.Count; i++)
            {
                executeCopiedOnMainThread[i]();
            }
        }
    }
}
