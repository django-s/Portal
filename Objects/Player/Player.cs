namespace Portal.Objects;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Portal.Autoloads;
using Portal.Maps.Assets;
using Portal.Objects.PortalTraveller;

public partial class Player : CharacterBody3D, IPortalTraveller
{
	[Export]
	private PortalManager portalManager;

	public Vector3 PreviousOffsetFromPortal { get; set; }

	private PortalSignals events;

	private Node3D head;
	private Camera3D camera;
	private RayCast3D rayCast;

	private const float Speed = 5;
	private const float GroundFriction = 0.5f;
	private const float AirFriction = 0.005f;
	private const float Gravity = 15;
	private const float MaxGravityVelocity = 35;
	private const float JumpVelocity = 6.5f;
	private const float MouseSensitivity = 0.01f;

	private Signal shootPortal;

	public override void _Ready()
	{
		events = GetNode<PortalSignals>("/root/PortalSignals");

		head = GetNode<Node3D>("Head");
		camera = GetNode<Camera3D>("Head/Camera");
		rayCast = GetNode<RayCast3D>("Head/RayCast");

		Input.MouseMode = Input.MouseModeEnum.Captured;

		FloorStopOnSlope = true;
		FloorSnapLength = 0.2f;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
			FpsHeadMovement(mouseMotion);
	}

	public override void _PhysicsProcess(double delta)
	{
		var direction = GetMovementDirection();
		UpdateVelocity(direction, (float)delta);

		MoveAndSlide();

		HandlePortalShooting();

		var distances = new List<float>();
		foreach (var portal in portalManager.PlacedPortals)
		{
			distances.Add((portal.GlobalPosition - GlobalPosition).Length());
		}

		var minDistance = float.MaxValue;
		if (distances.Count != 0)
			minDistance = distances.Min();

		if (minDistance < 0.5)
			camera.Near = 0.001f;
		else
			camera.Near = 0.05f;
	}

	private void HandlePortalShooting()
	{
		if (Input.IsActionJustPressed("ShootA"))
			OnShoot(PortalType.A);
		if (Input.IsActionJustPressed("ShootB"))
			OnShoot(PortalType.B);
	}

	private void OnShoot(PortalType portalType)
	{
		var collider = rayCast.GetCollider();
		if (collider is PortalableSurface surface)
		{
			// Get portal rotation
			//var portalRotation =
			//    rayCast.GetCollisionNormal().Cross(Vector3.Up).Length() < 0.01 ? Rotation.Y : 0;

			events.PortalShot(surface, rayCast.GetCollisionPoint(), portalType);
		}
	}

	private Vector3 GetMovementDirection()
	{
		var rotation = GlobalTransform.Basis.GetEuler().Y;
		var forwardInput =
			Input.GetActionStrength("MoveBackward") - Input.GetActionStrength("MoveForward");
		var horizontalInput =
			Input.GetActionStrength("MoveRight") - Input.GetActionStrength("MoveLeft");
		return new Vector3(horizontalInput, 0, forwardInput)
			.Rotated(Vector3.Up, rotation)
			.Normalized();
	}

	private void UpdateVelocity(Vector3 direction, float delta)
	{
		var friction = IsOnFloor() ? GroundFriction : AirFriction;
		Velocity = new Vector3(
			Velocity.X * (1 - friction),
			Velocity.Y,
			Velocity.Z * (1 - friction)
		);

		if (Velocity.Length() < Speed + 0.1)
			Velocity = new Vector3(direction.X * Speed, Velocity.Y, direction.Z * Speed);

		if (IsOnFloor() && Input.IsActionJustPressed("Jump"))
			Velocity = new Vector3(Velocity.X, JumpVelocity, Velocity.Z);

		if (!IsOnFloor() && Velocity.Y > -MaxGravityVelocity)
			Velocity += new Vector3(0, -Gravity * delta, 0);
	}

	private void FpsHeadMovement(InputEventMouseMotion mouseMotion)
	{
		RotateY(-mouseMotion.Relative.X * MouseSensitivity);
		head.RotateX(-mouseMotion.Relative.Y * MouseSensitivity);
		head.Rotation = new Vector3(
			x: (float)Math.Clamp(head.Rotation.X, (-Math.PI / 2) + 0.1, (Math.PI / 2) - 0.1),
			y: head.Rotation.Y,
			z: head.Rotation.Z
		);
	}

	public void Travel(Transform3D relativeTransform)
	{
		GD.Print("Travelling through portal");

		GlobalTransform = relativeTransform * GlobalTransform;
		Velocity = relativeTransform.Basis * Velocity;

		OrientPlayerUpwards(0.25f);
	}

	private void OrientPlayerUpwards(float duration)
	{
		var previousHeadRotation = head.GlobalRotation;
		Quaternion = new Quaternion(0, Quaternion.Y, 0, Quaternion.W); // Puts player on feet
		head.GlobalRotation = previousHeadRotation; // Maintain global head rotation

		// Want the player body, not the head to rotate around Y
		Rotation += new Vector3(0, head.Rotation.Y, 0);
		head.Rotation = new Vector3(head.Rotation.X, 0, head.Rotation.Z);

		// Tween the head z rotation, as it must be zero, but looks janky if you do it in one frame
		var tween = CreateTween().SetTrans(Tween.TransitionType.Quad);
		tween.TweenProperty(head, "rotation:z", 0, duration);
	}
}
