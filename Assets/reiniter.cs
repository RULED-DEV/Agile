using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class reiniter : MonoBehaviour
{
    private static reiniter _instance;

    public static reiniter Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("CoroutineRunner");
                _instance = go.AddComponent<reiniter>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
}
