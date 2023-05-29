namespace Portal.Autoloads;

using System;
using Godot;
using Portal.Maps.Assets;
using Portal.Objects;

public partial class PortalSignals : Node
{
    public Action<PortalableSurface, Vector3, PortalType> PortalShot { get; set; }
    public Action<PortalObject> PortalRemoved { get; set; }
}
