using System;
using System.Collections.Generic;
using System.Diagnostics;
using Evergine.Common.Graphics;
using Evergine.Common.Input.Pointer;
using Evergine.Components.Graphics3D;
using Evergine.Components.Primitives;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;

namespace EverSneaks.Components;

public class CameraBehavior : Behavior
{
    public float RotationSpeed = 0.2f;
    public float PanSpeed = 0.01f;
    public float ZoomSpeed = 0.5f;
        
    public float TouchSensibility { get; set; } = 0.5f;
    
    private PointerDispatcher pointerDispatcher;
    private Vector2? lastSingleTouch;
    private Vector2? lastTwoFingerMidpoint;
    private float lastTwoFingerDistance;

    private Vector3 target = Vector3.Zero;
    private float distanceToTarget = 10.0f;
    private float yaw = 0;
    private float pitch = 20;
    
    [BindComponent(source: BindComponentSource.ChildrenSkipOwner)]
    private Camera3D camera3D = null;

    [BindService]
    private GraphicsPresenter graphicsPresenter = null;


    private PointerDispatcher touchDispatcher;
    private Evergine.Mathematics.Point currentTouchState;
    private Vector2 lastTouchPosition;
    private Display display;


    protected override void Start()
    {
        base.Start();
    }

    protected override void Update(TimeSpan gameTime)
    {
        this.graphicsPresenter.TryGetDisplay("DefaultDisplay", out var presenterDisplay);
        if (presenterDisplay != this.display) 
        {
            this.camera3D.DisplayTagDirty = true;
            this.RefreshDisplay();
        };

        var touches = touchDispatcher.Points;

        if (touches.Count == 1)
        {
            var touch = touches[0];
            if (lastSingleTouch.HasValue)
            {
                var delta = touch.Position.ToVector2() - lastSingleTouch.Value;
                yaw -= delta.X * RotationSpeed;
                pitch -= delta.Y * RotationSpeed;
                pitch = MathHelper.Clamp(pitch, -89, 89);
            }
            lastSingleTouch = touch.Position.ToVector2();
            lastTwoFingerMidpoint = null;
        }
        else if (touches.Count == 2)
        {
            var p1 = touches[0].Position.ToVector2();
            var p2 = touches[1].Position.ToVector2();

            var midpoint = (p1 + p2) * 0.5f;
            var dist = Vector2.Distance(p1, p2);

            if (lastTwoFingerMidpoint.HasValue)
            {
                var panDelta = midpoint - lastTwoFingerMidpoint.Value;
                var camRight = camera3D.Transform.WorldTransform.Right;
                var camUp =  camera3D.Transform.WorldTransform.Up;

                target -= camRight * panDelta.X * PanSpeed;
                target += camUp * panDelta.Y * PanSpeed;

                var zoomDelta = dist - lastTwoFingerDistance;
                distanceToTarget -= zoomDelta * ZoomSpeed;
                distanceToTarget = MathHelper.Clamp(distanceToTarget, 1f, 5_000f);
            }

            lastTwoFingerMidpoint = midpoint;
            lastTwoFingerDistance = dist;
            lastSingleTouch = null;
        }
        else
        {
            lastSingleTouch = null;
            lastTwoFingerMidpoint = null;
        }

        // Update camera position
        var rotation = Quaternion.CreateFromYawPitchRoll(
            MathHelper.ToRadians(yaw),
            MathHelper.ToRadians(pitch),
            0f);

        var offset = Vector3.Transform(Vector3.Backward * distanceToTarget, rotation);
        camera3D.Transform.Position = target + offset;
        camera3D.Transform.LookAt(target, Vector3.Up);
    }
    
    

    private void RefreshDisplay()
    {
        UnbindTouchEvents();
            
        this.display = this.camera3D.Display;
        if (this.display != null)
        {
            this.touchDispatcher = this.display.TouchDispatcher;
        }
            
        BindTouchEvents();
    }

    private void UnbindTouchEvents()
    {
        if (touchDispatcher != null)
        {
            touchDispatcher.PointerDown -= OnPointerPressed;
            touchDispatcher.PointerMove -= OnPointerMoved;
            touchDispatcher.PointerUp -= OnPointerReleased;
        }
    }

    private void OnPointerReleased(object sender, PointerEventArgs e)
    {
    }

    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
    }

    private void OnPointerPressed(object sender, PointerEventArgs e)
    {
        if (touchDispatcher.Points.Count == 1)
        {
            TryRaycastFromTouch();
        }
    }
    
    public static Ray GetRayFromScreenPoint(Vector2 screenPoint, Camera3D camera, Vector2 screenSize)
    {
        // Normalize screen point to [-1, 1] range (clip space)
        float x = (2.0f * screenPoint.X) / screenSize.X - 1.0f;
        float y = 1.0f - (2.0f * screenPoint.Y) / screenSize.Y; // Flip Y axis
        Vector3 nearPoint = new Vector3(x, y, 0f);
        Vector3 farPoint = new Vector3(x, y, 1f);

        // Inverse ViewProjection
        var view = camera.View;
        var projection = camera.Projection;
        var viewProjectionInverse = (view * projection);
        viewProjectionInverse.Invert();

        // Unproject points
        Vector3 worldNear = Vector3.TransformCoordinate(nearPoint, viewProjectionInverse);
        Vector3 worldFar = Vector3.TransformCoordinate(farPoint, viewProjectionInverse);

        // Ray direction
        Vector3 direction = Vector3.Normalize(worldFar - worldNear);
    
        return new Ray(worldNear, direction);
    }
    
    public void TryRaycastFromTouch()
    {
        var screenPos = touchDispatcher.Points[0].Position;

        var display = camera3D.Display;

        Vector2 ndc = new Vector2(
            ((float)screenPos.X / (float)display.Width) * 2f - 1f,
            1f - ((float)screenPos.Y / (float)display.Height) * 2f
        );

        var ray = GetRayFromScreenPoint(screenPos.ToVector2(), camera3D, new Vector2(display.Width, display.Height));

        var colliders = this.Managers.PhysicManager3D.PhysicComponentList;
            
        var hitResult = this.Managers.PhysicManager3D.RayCast(ref ray, float.MaxValue);

        if (hitResult.Succeeded)
        {
            Debug.WriteLine($"Hit entity: {hitResult.Collider}, point: {hitResult.Point}, normal: {hitResult.Normal}");
        }
        
        DrawDebugLine(ray.Position, ray.Position + (ray.Direction * 100));
    }
    
    private void DrawDebugLine(Vector3 start, Vector3 end)
    {
        // Create the LineMesh
        var lineEntity = new Entity("DebugLine")
            .AddComponent(new Transform3D())
            .AddComponent(new MeshRenderer())
            .AddComponent(new LineMesh());
        
        
        var lineMesh = new LineMesh();

        lineMesh.LineType = LineType.LineStrip;
        lineMesh.IsLoop = false;

        List<LinePointInfo> linePoints = new List<LinePointInfo>()
        {
            new LinePointInfo()
            {
                Position = start,
                Color = Color.Green,
                Thickness = 0.5f
            },

            new LinePointInfo()
            {
                Position = end,
                Color = Color.Green,
                Thickness = 0.5f
            }
        };

        lineMesh.IsCameraAligned = true;
        lineMesh.LinePoints = linePoints;

        var line = new Entity("DebugLine")
            .AddComponent(new Transform3D())
            .AddComponent(lineMesh)
            .AddComponent(new LineMeshRenderer3D()
            {
                CastShadows = false,
            });
        
        
        Owner.Scene.Managers.EntityManager.Add(line);
    }

    private void BindTouchEvents()
    {
        UnbindTouchEvents();
            
        if (touchDispatcher != null)
        {
            touchDispatcher.PointerDown += OnPointerPressed;
            touchDispatcher.PointerMove += OnPointerMoved;
            touchDispatcher.PointerUp += OnPointerReleased;
        }
    }

}
