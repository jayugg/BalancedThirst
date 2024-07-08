using System.Collections.Generic;
using Vintagestory.API.Common;

namespace BalancedThirst.Util;

public static class ShapeUtilExtensions
{
    public static Shape FlattenHierarchy(this Shape shape)
    {
        shape.Elements = FlattenHierarchy(shape.Elements).ToArray();
        return shape;
    }
    
    public static List<ShapeElement> FlattenHierarchy(ShapeElement[] rootElements)
    {
        var flatList = new List<ShapeElement>();
        foreach (var element in rootElements)
        {
            FlattenHierarchyHelper(element, flatList);
        }
        return flatList;
    }

    private static void FlattenHierarchyHelper(ShapeElement element, List<ShapeElement> flatList)
    {
        flatList.Add(element);
        if (element.Children != null)
        {
            foreach (var child in element.Children)
            {
                FlattenHierarchyHelper(child, flatList);
            }
        }
    }
}