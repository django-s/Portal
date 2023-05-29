namespace Portal.Objects.PortalTraveller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

internal interface IPortalTraveller
{
    public Vector3 PreviousOffsetFromPortal { get; set; }
    public void Travel(Transform3D relativeTransform);
}
