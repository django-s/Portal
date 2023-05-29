namespace Portal.ExtensionMethods;

using System;
using Godot;
using System.Linq;
using System.Collections.Generic;

public static class VectorExtensionMethods
{
    public static IEnumerable<Vector2> RemoveAxis(
        this IEnumerable<Vector3> vertices,
        Vector3.Axis axisToRemove
    )
    {
        return vertices.Select(n => RemoveAxis(n, axisToRemove));
    }

    public static Vector2 RemoveAxis(this Vector3 vertex, Vector3.Axis axisToRemove)
    {
        return axisToRemove switch
        {
            Vector3.Axis.X => new Vector2(vertex.Y, vertex.Z),
            Vector3.Axis.Y => new Vector2(vertex.X, vertex.Z),
            Vector3.Axis.Z => new Vector2(vertex.X, vertex.Y),
            _ => throw new ArgumentOutOfRangeException(nameof(axisToRemove)),
        };
    }
}
