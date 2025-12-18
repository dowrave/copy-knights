using UnityEngine;
using System.Runtime.CompilerServices;

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

    // 변수를 넣으면 그 변수의 이름과 값을 출력하는 코드
    // fieldName에는 CallerArgumentExpression이라는, 변수 이름을 그대로 string으로 바꾸는 어트리뷰트가 적용된다.
    // 다만 CallerArgumentExpression
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogFieldStatus(object fieldValue, [CallerArgumentExpression("fieldValue")] string fieldName = "")
    {
        // Debug.LogError(message);
        Debug.Log($"{fieldName} : {fieldValue}");
    }
}
