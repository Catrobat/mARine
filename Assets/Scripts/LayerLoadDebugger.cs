using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class LayerLoadDebugger
{
    public static void Log(string msg)
    {
        Debug.Log($"[LayerDebug] {msg}");
    }

    public static void TestInstantiate(string key, Transform parent)
    {
        Log("----------------------------------------");
        Log($"Testing Addressable key : {key}");
        Log($"Parent                 : {(parent != null ? parent.name : "NULL")}");

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(key, parent);

        Log($"Handle created");
        Log($"Initial Status = {handle.Status}");
        Log($"Initial IsDone = {handle.IsDone}");
        Log($"Percent Complete = {handle.PercentComplete}");

        DebugRunner.Instance.StartCoroutine(WatchHandle(handle));

        handle.Completed += op =>
        {
            Log("===== COMPLETED CALLBACK =====");
            Log($"Status = {op.Status}");
            Log($"IsDone = {op.IsDone}");
            Log($"Percent = {op.PercentComplete}");

            if (op.OperationException != null)
            {
                Debug.LogError($"[LayerDebug] Exception:\n{op.OperationException}");
            }

            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject obj = op.Result;

                Log($"Loaded Object = {obj.name}");
                Log($"World Position = {obj.transform.position}");
                Log($"Local Position = {obj.transform.localPosition}");
                Log($"Child Count = {obj.transform.childCount}");

                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

                Log($"Renderer Count = {renderers.Length}");
            }

            Log("==============================");
        };
    }

    private static IEnumerator WatchHandle(AsyncOperationHandle<GameObject> handle)
    {
        float elapsed = 0f;

        while (!handle.IsDone && elapsed < 20f)
        {
            Log($"Waiting... {elapsed:F1}s | Status={handle.Status} | Percent={handle.PercentComplete:P1}");

            elapsed += 1f;
            yield return new WaitForSeconds(1f);
        }

        Log("Watch Finished");
        Log($"Final Status = {handle.Status}");
        Log($"Final IsDone = {handle.IsDone}");
        Log($"Final Percent = {handle.PercentComplete}");

        if (handle.OperationException != null)
        {
            Debug.LogError($"[LayerDebug] Final Exception:\n{handle.OperationException}");
        }
    }
}