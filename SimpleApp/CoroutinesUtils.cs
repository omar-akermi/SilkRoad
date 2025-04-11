using System.Collections;
using UnityEngine;

public static class CoroutineUtils
{
    private static MonoBehaviour runner;

    public static void Run(IEnumerator routine)
    {
        if (runner == null)
        {
            GameObject go = new GameObject("CoroutineRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<MonoBehaviourRunner>();
        }
        runner.StartCoroutine(routine);
    }

    private class MonoBehaviourRunner : MonoBehaviour { }
}
