namespace Portal.Objects;

using System;
using System.Collections.Generic;
using Godot;
using Portal.Maps.Assets;
using Portal.Objects.PortalTraveller;

public partial class PortalObject : Node3D
{
    private PortalObject linkedPortal;
    public PortalObject LinkedPortal
    {
        get => linkedPortal;
        set
        {
            linkedPortal = value;

            // Don't allow player through portal if there is not a linked portal
            // Switch out between portal and unconnected materials as well.
            if (value == null)
            {
                portalBlocker.CollisionLayer = 1;
                portalMesh.MaterialOverride = unlinkedMaterial;
            }
            else
            {
                portalBlocker.CollisionLayer = 0;
                portalMesh.MaterialOverride = portalMaterial;
            }
        }
    }

    private PortalType portalType;
    public PortalType PortalType
    {
        get => portalType;
        set
        {
            portalType = value;
            if (portalType == PortalType.B)
            {
                portalMesh.Position = new Vector3(
                    portalMesh.Position.X,
                    portalMesh.Position.Y,
                    -portalMesh.Position.Z
                );
            }
            UpdatePortalColour();
        }
    }

    public Camera3D PlayerCamera { get; set; }
    public PortalableSurface AttachedSurface { get; set; }

    private SubViewport portalViewport;
    private Camera3D portalCamera;
    private MeshInstance3D portalMesh;
    private StaticBody3D portalBlocker;
    private ShaderMaterial portalMaterial;
    private BaseMaterial3D unlinkedMaterial;

    private readonly List<IPortalTraveller> trackedPortalTravellers = new();

    public override void _Ready()
    {
        portalViewport = GetNode<SubViewport>("PortalViewport");
        portalCamera = portalViewport.GetNode<Camera3D>("PortalCamera");
        portalMesh = GetNode<MeshInstance3D>("PortalMesh");
        portalBlocker = GetNode<StaticBody3D>("PortalBlocker");

        portalMaterial = (ShaderMaterial)
            GD.Load("res://Objects/PortalObject/PortalMaterial.tres").Duplicate();
        portalMaterial.SetShaderParameter("TextureAlbedo", portalViewport.GetTexture());
        unlinkedMaterial = (BaseMaterial3D)
            GD.Load("res://Objects/PortalObject/UnlinkedMaterial.tres");
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateViewPortSize();
        UpdatePortalCameraTransform();
        //UpdatePortalCameraCulling();

        DetectPortalUse();
    }

    private void UpdatePortalColour()
    {
        if (PortalType == PortalType.A)
            unlinkedMaterial.AlbedoColor = Colors.Blue;
        else
            unlinkedMaterial.AlbedoColor = Colors.Red;
    }

    private void DetectPortalUse()
    {
        var portalNormal = GlobalTransform.Basis * Vector3.Forward;
        foreach (var portalTraveller in trackedPortalTravellers)
        {
            var nPortalTraveller = portalTraveller as Node3D;

            var offsetFromPortal = nPortalTraveller.GlobalPosition - GlobalPosition;
            var currentPortalSide = Math.Sign(offsetFromPortal.Dot(portalNormal));
            var previousPortalSide = Math.Sign(
                portalTraveller.PreviousOffsetFromPortal.Dot(portalNormal)
            );

            if (currentPortalSide != previousPortalSide)
            {
                portalTraveller.Travel(GetRelativeTransformToLinkedPortal());
                portalTraveller.PreviousOffsetFromPortal =
                    nPortalTraveller.GlobalPosition - GlobalPosition;
            }
            else
            {
                portalTraveller.PreviousOffsetFromPortal = offsetFromPortal;
            }
        }
    }

    // TODO: Bind this to a signal generated when size changes
    private void UpdateViewPortSize()
    {
        portalViewport.Size = (GetViewport() as Window).Size;
    }

    private void UpdatePortalCameraTransform()
    {
        if (LinkedPortal == null)
            return;

        portalCamera.GlobalTransform =
            GetRelativeTransformToLinkedPortal() * PlayerCamera.GlobalTransform;
    }

    private Transform3D GetRelativeTransformToLinkedPortal()
    {
        return LinkedPortal.GlobalTransform * GlobalTransform.Inverse();
    }

    private void UpdatePortalCameraCulling()
    {
        // TODO: Implment oblique
        throw new NotImplementedException();
    }

    // Linked to body_entered signal from Area3D
    private void OnBodyEntered(Node3D body)
    {
        if (body is IPortalTraveller portalTraveller)
        {
            trackedPortalTravellers.Add(portalTraveller);
            portalTraveller.PreviousOffsetFromPortal = body.GlobalPosition - GlobalPosition;
        }
    }

    // Linked to body_exited signal from Area3D
    private void OnBodyExited(Node3D body)
    {
        trackedPortalTravellers.Remove(body as IPortalTraveller);
    }

    public static Polygon2D GetPortalPolygon()
    {
        var width = 1.8f;
        var height = 3f;

        return new Polygon2D()
        {
            Polygon = new Vector2[]
            {
                // TODO: Make this not hard coded
                new Vector2(width / 2, height / 2),
                new Vector2(-width / 2, height / 2),
                new Vector2(-width / 2, -height / 2),
                new Vector2(width / 2, -height / 2)
            }
        };
    }
}

public enum PortalType
{
    A,
    B
}
