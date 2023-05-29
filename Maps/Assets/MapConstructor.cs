namespace Portal.Maps.Assets;

using Godot;
using Portal.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MapConstructor : Node3D
{
    private List<MeshInstance3D> meshInstances = new();

    public override void _Ready()
    {
        GetChildMeshInstances();

        // TODO: Neaten this and check for prefixes
        foreach (var mesh in meshInstances.Select(n => n.Mesh))
        {
            //var points = mesh.GetFaces();
            var triangles = mesh.GetFaces()
                .Select((s, i) => new { Value = s, Index = i })
                .GroupBy(x => x.Index / 3)
                .Select(grp => grp.Select(x => x.Value).ToArray())
                .ToList();

            // Remove triangle slithers
            triangles.RemoveAll(n => !IsValidTriangle(n));

            var coplanarTriangles = GetCoplanarTriangles(triangles.ToArray());

            var portalableSurfaceScene = GD.Load<PackedScene>(
                "res://Maps/Assets/PortalableSurface.tscn"
            );

            foreach (var face in coplanarTriangles)
            {
                var transform = ResetFaceTransform(face);

                var portalableSurface = portalableSurfaceScene.Instantiate() as PortalableSurface;
                portalableSurface.Triangles = face;
                portalableSurface.Transform = transform;
                AddChild(portalableSurface);
            }
        }
    }

    private static bool IsValidTriangle(IEnumerable<Vector3> triangle)
    {
        var triangleArray = triangle.ToArray();

        if (triangleArray.Length != 3)
            return false;

        // Any points are equal
        if (
            triangleArray[0].IsEqualApprox(triangleArray[1])
            || triangleArray[0].IsEqualApprox(triangleArray[2])
            || triangleArray[1].IsEqualApprox(triangleArray[2])
        )
            return false;

        // In straight line
        // TODO

        return true;
    }

    private static Transform3D ResetFaceTransform(Vector3[][] triangles)
    {
        var normal = GetNormal(triangles[0]);
        var minimumDistanceToOrigin = normal.Dot(triangles[0][0]);
        var originalTransform = Transform3D.Identity;

        if (normal.Cross(Vector3.Up).Length() < 0.01) // Floor/Cieling
            originalTransform = originalTransform.LookingAt(normal * 1000000, Vector3.Forward);
        else
            originalTransform = originalTransform.LookingAt(normal * 1000000, Vector3.Up);

        originalTransform = originalTransform.Translated(minimumDistanceToOrigin * normal);

        foreach (var triangle in triangles)
        {
            for (var i = 0; i < triangle.Length; i++)
            {
                triangle[i] = originalTransform.Inverse() * triangle[i];
            }
        }

        return originalTransform;
    }

    private static Vector3 GetNormal(IEnumerable<Vector3> triangle)
    {
        var triangleArray = triangle.ToArray();

        var line1 = triangleArray[1] - triangleArray[0];
        var line2 = triangleArray[2] - triangleArray[0];

        return line2.Cross(line1).Normalized();
    }

    private static Vector3[][][] GetCoplanarTriangles(Vector3[][] triangles)
    {
        var coplanarFaces = new Dictionary<Vector4, List<Vector3[]>>(); // Dictionary: keys define plane, values are all triangles on that plane

        foreach (var triangle in triangles)
        {
            var normal = GetNormal(triangle);
            var d = normal.Dot(triangle[0]);
            var plane = new Vector4(normal.X, normal.Y, normal.Z, d);

            if (coplanarFaces.Keys.Any(key => key.IsEqualApprox(plane)))
            {
                var key = coplanarFaces.Keys.Single(key => key.IsEqualApprox(plane));
                coplanarFaces[key].Add(triangle);
            }
            else
            {
                coplanarFaces.Add(plane, new List<Vector3[]> { triangle });
            }
        }

        return coplanarFaces.Values.Select(n => n.ToArray()).ToArray();
    }

    private void GetChildMeshInstances()
    {
        foreach (var child in GetChildren())
        {
            if (child is MeshInstance3D mChild)
            {
                meshInstances.Add(mChild);
            }
        }
    }
}
