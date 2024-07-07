using System;
using System.Reflection;

namespace BalancedThirst.Util;

public static class ReflectionExtensions
{
    public static T GetField<T>(this object obj, string fieldName)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));

        FieldInfo fi = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (fi == null) return default;

        return (T)fi.GetValue(obj);
    }

}