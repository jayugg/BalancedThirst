using System;
using System.Reflection;
using HarmonyLib;

namespace BalancedThirst.Util;

public class ManualPatch
  {
    internal static void PatchMethod(
      Harmony harmony,
      Type type,
      Type patch,
      string methodName,
      string prefixName,
      string postfixName)
    {
      if (harmony == null || type == (Type) null || patch == (Type) null || methodName == null)
        return;
      Type type1 = type;
      MethodInfo methodInfo;
      do
      {
        MethodInfo method = type1.GetMethod(methodName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if ((object) method == null)
          method = type1.GetMethod("get_" + methodName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        methodInfo = method;
        type1 = type1.BaseType;
      }
      while (type1 != (Type) null && methodInfo == (MethodInfo) null);
      if (methodInfo == (MethodInfo) null)
        return;
      BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
      MethodInfo method1 = prefixName != null ? patch.GetMethod(prefixName, bindingAttr) : (MethodInfo) null;
      MethodInfo method2 = postfixName != null ? patch.GetMethod(postfixName, bindingAttr) : (MethodInfo) null;
      HarmonyMethod harmonyMethod1 = method1 != (MethodInfo) null ? new HarmonyMethod(method1) : (HarmonyMethod) null;
      HarmonyMethod harmonyMethod2 = method2 != (MethodInfo) null ? new HarmonyMethod(method2) : (HarmonyMethod) null;
      harmony.Patch((MethodBase) methodInfo, harmonyMethod1, harmonyMethod2, (HarmonyMethod) null, (HarmonyMethod) null);
    }

    internal static void PatchMethod(Harmony harmony, Type type, Type patch, string method)
    {
      ManualPatch.PatchMethod(harmony, type, patch, method, method + "Prefix", method + "Postfix");
    }

    internal static void PatchConstructor(Harmony harmony, Type type, Type patch)
    {
      if (harmony == null || type == (Type) null)
        return;
      Type type1 = type;
      ConstructorInfo constructor;
      do
      {
        constructor = type1.GetConstructor(new Type[0]);
        type1 = type1.BaseType;
      }
      while (type1 != (Type) null && constructor == (ConstructorInfo) null);
      if (constructor == (ConstructorInfo) null)
        return;
      MethodInfo method1 = patch.GetMethod("ConstructorPrefix", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
      MethodInfo method2 = patch.GetMethod("ConstructorPostfix", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
      HarmonyMethod harmonyMethod1 = method1 != (MethodInfo) null ? new HarmonyMethod(method1) : (HarmonyMethod) null;
      HarmonyMethod harmonyMethod2 = method2 != (MethodInfo) null ? new HarmonyMethod(method2) : (HarmonyMethod) null;
      harmony.Patch((MethodBase) constructor, harmonyMethod1, harmonyMethod2, (HarmonyMethod) null, (HarmonyMethod) null);
    }
  }