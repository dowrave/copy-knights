using System;

public static class InstanceValidator
{
    public static void ValidateInstance<T>(T? instance)
    {
        if (instance == null)
        {
            throw new InvalidOperationException($"{nameof(instance)}이 null일 수 없습니다.");
        }
    }
}
