namespace Portal.Maps.Assets;

using System;
using System.Collections.Generic;
using System.Linq;
using Clipper2Lib;
using Godot;
using Portal.Autoloads;
using Portal.ExtensionMethods;
using Portal.Objects;

public partial class PortalableSurface : Node3D
{
    public List<PortalObject> AttachedPortals { get; set; } = new();

    // TODO: It's quite jank having this exposed.
    public Polygon2D[] CollisionPolygon
    {
        get => collisionPolygon;
        set
        {
            collisionPolygon = value;
            SetCollisionShapeFromPolygon(value);
        }
    }
    private Polygon2D[] collisionPolygon;

    public Vector3[][] Triangles
    {
        get => triangles;
        set
        {
            triangles = value;

            // Used for calculating updated collision with placedPortals cut out.
            CollisionPolygon = GetPolygonFromTriangles(triangles, Vector3.Axis.Z);
            // For portal raycast detection, remains unchanged.
            AddChild(GetCollisionShapeFromTriangles(Triangles));

            // For player collision. Changes when placedPortals are added
            var staticBody = new StaticBody3D();
            staticBody.AddChild(currentCollisionShape);
            AddChild(staticBody);
        }
    }
    private Vector3[][] triangles;

    private PortalSignals portalSignals;
    private CollisionShape3D currentCollisionShape = new();

    public override void _Ready() { }

    public void RemovePortal(PortalObject portal)
    {
        if (!AttachedPortals.Contains(portal))
            return;

        AttachedPortals.Remove(portal);

        CollisionPolygon = GetPolygonFromTriangles(Triangles, Vector3.Axis.Z);
        foreach (var attachedPortal in AttachedPortals)
        {
            AddPortalHoleToCollisionPolygon(attachedPortal.Position.RemoveAxis(Vector3.Axis.Z));
        }
    }

    public PortalObject PlacePortal(Vector3 suggestedGlobalPortalPosition)
    {
        var suggestedPortalPosition = GlobalTransform.Inverse() * suggestedGlobalPortalPosition;
        var portalPositionNullable = FindValidPortalPosition(
            suggestedPortalPosition.RemoveAxis(Vector3.Axis.Z)
        );

        if (portalPositionNullable == null)
            return null;

        var portalPosition2 = portalPositionNullable.Value;
        var wallOffset = -0.101f;
        var portalPosition3 = new Vector3(portalPosition2.X, portalPosition2.Y, wallOffset);

        var portal = AddNewPortal(portalPosition3);
        AddPortalHoleToCollisionPolygon(portalPosition2);

        return portal;
    }

    private void AddPortalHoleToCollisionPolygon(Vector2 portalPosition)
    {
        var offSetPortalPolygon = PortalObject
            .GetPortalPolygon()
            .Polygon.Select(n => n + portalPosition)
            .ToPolgyon();

        CollisionPolygon = CollisionPolygon
            .Clip(new List<Polygon2D>() { offSetPortalPolygon })
            .ToArray();
    }

    private void SetCollisionShapeFromPolygon(IEnumerable<Polygon2D> polygon)
    {
        var triangulatedPolygon = polygon.Triangulate();

        // Set new collisionshape
        var data = triangulatedPolygon
            .SelectMany(n => n.Polygon.Select(m => new Vector3(m.X, m.Y, 0)))
            .ToArray();

        var collisionShape = new ConcavePolygonShape3D { Data = data, BackfaceCollision = true };
        currentCollisionShape.Shape = collisionShape;
    }

    private PortalObject AddNewPortal(Vector3 portalPosition)
    {
        var portalScene = GD.Load<PackedScene>("res://Objects/PortalObject/PortalObject.tscn");
        var portal = portalScene.Instantiate() as PortalObject;
        portal.Position = portalPosition;
        AttachedPortals.Add(portal);
        portal.AttachedSurface = this;

        AddChild(portal);

        return portal;
    }

    private static CollisionShape3D GetCollisionShapeFromTriangles(Vector3[][] triangles)
    {
        return new CollisionShape3D()
        {
            Shape = new ConcavePolygonShape3D()
            {
                Data = triangles.SelectMany(n => n.Select(m => m)).ToArray()
            }
        };
    }

    private static Polygon2D[] GetPolygonFromTriangles(
        Vector3[][] triangles,
        Vector3.Axis axisToRemove
    )
    {
        var triangles2 = triangles.SelectMany(
            n => triangles.Select(m => m.RemoveAxis(axisToRemove))
        );
        return triangles2
            .Select(n => new Polygon2D() { Polygon = n.ToArray() })
            .ToPolygon()
            .ToArray();
    }

    private Vector2? FindValidPortalPosition(Vector2 suggestedPortalPosition)
    {
        var margin = 0.05f;
        var portalPolygon = PortalObject
            .GetPortalPolygon()
            .Inflate(margin)
            .Polygon.Select(n => n + suggestedPortalPosition)
            .ToPolgyon();

        var portalPositionOffset = FindClosestValidPortalPositionOffset(
            collisionPolygon,
            portalPolygon
        );

        return suggestedPortalPosition + portalPositionOffset;
    }

    private static Vector2? FindClosestValidPortalPositionOffset(
        Polygon2D[] baseCollisionPolygons,
        Polygon2D portalPolygon
    )
    {
        var directionNum = 32;
        var layerNum = 100;
        var maxMovement = 2.0f;
        var layerWidth = maxMovement / layerNum;

        var offsetGuessDirections = new List<Vector2>();

        for (var i = 0; i < directionNum; i++)
        {
            offsetGuessDirections.Add(
                new Vector2(1, 0).Rotated((float)(2 * i * Math.PI / directionNum))
            );
        }

        var offsetGuesses = new List<Vector2>() { new Vector2(0, 0) };

        for (var i = 0; i < layerNum; i++)
        {
            offsetGuesses.AddRange(offsetGuessDirections.Select(n => n * layerWidth * (i + 1)));
        }

        var baseCollisionPaths = baseCollisionPolygons.ToPath();
        var portalPath = portalPolygon.ToPath();

        foreach (var offsetGuess in offsetGuesses)
        {
            var offSetPortalPath = new PathsD(
                new List<PathD>
                {
                    new PathD(
                        portalPath.Select(n => new PointD(n.x + offsetGuess.X, n.y + offsetGuess.Y))
                    )
                }
            );

            if (offSetPortalPath.IsApproximatelyEntirelyContainedBy(baseCollisionPaths, 0.05))
                return offsetGuess;
        }

        return null;
    }
}
