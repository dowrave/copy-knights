using UnityEngine;

public static class Logger
{
    // [System.Diagnostics.Conditional("UNITY_EDITOR")]
    // 이 속성은 "UNITY_EDITOR" 심볼이 정의되었을 때만 이 메소드 호출 코드를 컴파일에 포함시킵니다.
    // 즉, 빌드 시에는 MyLogger.Log(...) 호출 자체가 사라집니다. #if보다 훨씬 깔끔합니다.
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(object message)
    {
        Debug.Log(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogWarning(object message)
    {
        Debug.LogWarning(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogError(object message)
    {
        Debug.LogError(message);
    }
    
    // 에러 로그는 빌드에서도 보고 싶을 경우, 속성을 제거하면 됩니다.
    // public static void LogError(object message)
    // {
    //     Debug.LogError(message);
    // }
}