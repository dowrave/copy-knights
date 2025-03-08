using UnityEngine;
using System;
using System.Diagnostics.CodeAnalysis;

public static class InstanceValidator
{
    public static void ValidateInstance<T>(T? instance)
    {
        if (instance == null)
        {
            throw new InvalidOperationException($"{nameof(instance)}∞° null¿”");
        }
    }
}
