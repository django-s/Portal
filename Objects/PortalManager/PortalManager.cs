namespace Portal.Objects;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Portal.Autoloads;
using Portal.Maps.Assets;

public partial class PortalManager : Node
{
    public List<PortalObject> PlacedPortals { get; set; } = new();

    [Export]
    private Player player;
    private Camera3D playerCamera;
    private PortalSignals portalSignals;

    public override void _Ready()
    {
        // TODO: Remove tight coupling to player here. Perhaps search through entire scene
        playerCamera = player.GetNode<Camera3D>("Head/Camera");

        portalSignals = GetNode<PortalSignals>("/root/PortalSignals");
        portalSignals.PortalShot += OnPortalShot;
    }

    private void OnPortalShot(
        PortalableSurface surface,
        Vector3 suggestedGlobalPosition,
        PortalType portalType
    )
    {
        foreach (var placedPortal in PlacedPortals.Where(n => n.PortalType == portalType)) // Should only have 0 or 1
        {
            placedPortal.AttachedSurface.RemovePortal(placedPortal);
            placedPortal.QueueFree();
        }
        PlacedPortals.RemoveAll(n => n.PortalType == portalType);

        var portal = surface.PlacePortal(suggestedGlobalPosition);

        if (portal == null)
        {
            LinkPlacedPortals();
            return;
        }

        portal.PortalType = portalType;
        portal.PlayerCamera = playerCamera;
        PlacedPortals.Add(portal);

        if (portalType == PortalType.B)
            portal.RotateY((float)Math.PI);

        LinkPlacedPortals();
    }

    private void LinkPlacedPortals()
    {
        if (PlacedPortals.Count != 2)
        {
            foreach (var placedPortal in PlacedPortals)
                placedPortal.LinkedPortal = null;

            return;
        }

        PlacedPortals[0].LinkedPortal = PlacedPortals[1];
        PlacedPortals[1].LinkedPortal = PlacedPortals[0];
    }
}
