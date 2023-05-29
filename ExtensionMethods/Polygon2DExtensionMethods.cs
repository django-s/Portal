namespace Portal.ExtensionMethods;

using System.Collections.Generic;
using System.Linq;
using Clipper2Lib;
using Godot;
using Poly2Tri.Triangulation.Polygon;
using Poly2Tri;

internal static class Polygon2DExtensionMethods
{
    public static IEnumerable<Polygon2D> ToPolygon(this IEnumerable<Polygon2D> triangles)
    {
        var trianglePaths = triangles.ToPath();
        var polygons = Clipper.Union(trianglePaths, FillRule.NonZero);

        return polygons.Select(n => n.ToPolygon());
    }

    public static Polygon2D ToPolygon(this PathD path)
    {
        return new Polygon2D()
        {
            Polygon = path.Select(n => new Vector2((float)n.x, (float)n.y)).ToArray()
        };
    }

    public static IEnumerable<Polygon2D> ToPolygon(this PathsD paths)
    {
        return paths.Select(n => n.ToPolygon());
    }

    public static Polygon2D ToPolgyon(this IEnumerable<Vector2> points)
    {
        return new Polygon2D() { Polygon = points.ToArray() };
    }

    public static IEnumerable<Polygon2D> Clip(
        this IEnumerable<Polygon2D> polygonA,
        IEnumerable<Polygon2D> polygonB
    )
    {
        var pathA = polygonA.ToPath();
        var pathB = polygonB.ToPath();

        return Clipper.Difference(pathA, pathB, FillRule.NonZero).ToPolygon();
    }

    public static IEnumerable<Polygon2D> Triangulate(this IEnumerable<Polygon2D> polygons)
    {
        var positivePolygons = polygons.Where(n => Clipper.IsPositive(n.ToPath()));
        var negativePolygons = polygons.Where(n => !Clipper.IsPositive(n.ToPath()));

        var triangles = new List<Polygon2D>();
        foreach (var positivePolygon in positivePolygons)
        {
            var polygon = new Polygon(
                positivePolygon.Polygon.Select(point => new PolygonPoint(point.X, point.Y))
            );

            foreach (var negativePolygon in negativePolygons)
                if (
                    negativePolygon
                        .ToPath()
                        .ToPathsD()
                        .IsApproximatelyEntirelyContainedBy(
                            positivePolygon.ToPath().ToPathsD(),
                            0.05f
                        )
                )
                {
                    polygon.AddHole(
                        new Polygon(
                            negativePolygon.Polygon.Select(
                                point => new PolygonPoint(point.X, point.Y)
                            )
                        )
                    );
                }

            P2T.Triangulate(polygon);
            triangles.AddRange(
                polygon.Triangles.Select(
                    n =>
                        new Polygon2D()
                        {
                            Polygon = n.Points
                                .Select(p => new Vector2((float)p.X, (float)p.Y))
                                .ToArray()
                        }
                )
            );
        }

        return triangles;
    }

    public static Polygon2D Inflate(this Polygon2D polygon, float delta)
    {
        var path = new PathsD { polygon.ToPath() };
        return Clipper
            .InflatePaths(path, delta, JoinType.Square, EndType.Polygon)
            .ToPolygon()
            .First();
    }

    public static IEnumerable<Polygon2D> Inflate(this IEnumerable<Polygon2D> polygons, float delta)
    {
        var paths = polygons.ToPath();
        return Clipper.InflatePaths(paths, delta, JoinType.Square, EndType.Polygon).ToPolygon();
    }

    public static PathD ToPath(this Polygon2D polygon)
    {
        return new PathD(polygon.Polygon.Select(n => new PointD(n.X, n.Y)));
    }

    public static PathsD ToPath(this IEnumerable<Polygon2D> polygons)
    {
        return new PathsD(polygons.Select(n => n.ToPath()));
    }

    public static bool IsApproximatelyEqualTo(this PathsD left, PathsD right, double areaDelta)
    {
        var difference = Clipper.Difference(left, right, FillRule.NonZero);
        return Clipper.Area(difference) < areaDelta;
    }

    public static bool IsEqualTo(this PathsD left, PathsD right)
    {
        return left.IsApproximatelyEqualTo(right, 0);
    }

    public static bool IsEntirelyContainedBy(this PathsD inner, PathsD outer)
    {
        return Clipper.Union(inner, outer, FillRule.NonZero).IsEqualTo(outer);
    }

    public static bool IsApproximatelyEntirelyContainedBy(
        this PathsD inner,
        PathsD outer,
        double areaDelta
    )
    {
        return Clipper
            .Union(inner, outer, FillRule.NonZero)
            .IsApproximatelyEqualTo(outer, areaDelta);
    }

    public static PathsD ToPathsD(this PathD path)
    {
        return new PathsD(new List<PathD>() { path });
    }
}
