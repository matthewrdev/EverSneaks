// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common;
using Evergine.Common.Graphics;
using Evergine.Common.Input;
using Evergine.Common.Input.Mouse;
using Evergine.Common.Input.Pointer;
using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System;
using System.Diagnostics;
using System.Linq;
using Evergine.Framework.Particles.Helpers;
using Evergine.Framework.Physics3D;
using static EverSneaks.Services.ControllerService;

namespace EverSneaks.Components
{
    public class CameraBehavior : Behavior
    {
        public float RotationSpeed = 0.2f;
        public float PanSpeed = 0.01f;
        public float ZoomSpeed = 0.5f;
        
        public float TouchSensibility { get; set; } = 0.5f;

        private Vector2? lastPosition;
        private bool isRotating;
        private bool isPanning;
        private float lastPinchDistance;
        
        /// <summary>
        /// The camera to move.
        /// </summary>
        [BindComponent(false)]
        public Transform3D Transform = null;

        /// <summary>
        /// The child transform.
        /// </summary>
        private Transform3D childTransform;

        /// <summary>
        /// The camera transform.
        /// </summary>
        private Transform3D cameraTransform;


        /// <summary>
        /// The orbit_scale.
        /// </summary>
        private const float OrbitScale = 0.005f;

        /// <summary>
        /// The is dirty.
        /// </summary>
        private bool isDirty;

        /// <summary>
        /// The current mouse state.
        /// </summary>
        private Vector3 cameraInitialPosition;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner)]
        private Camera3D camera3D = null;

        [BindService]
        private GraphicsPresenter graphicsPresenter = null;


        private PointerDispatcher touchDispatcher;
        private Evergine.Mathematics.Point currentTouchState;
        private Vector2 lastTouchPosition;
        private Display display;

        public CameraBehavior()
        {
        }

        /// <inheritdoc/>
        protected override void OnLoaded()
        {
            base.OnLoaded();

            this.isRotating = false;

            this.isDirty = true;
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            var child = this.Owner.ChildEntities.First();
            this.childTransform = child.FindComponent<Transform3D>();
            this.cameraTransform = child.ChildEntities.First().FindComponent<Transform3D>();

            this.cameraInitialPosition = this.cameraTransform.LocalPosition;

            return base.OnAttached();
        }

        protected override void OnDetach()
        {
            UnbindTouchEvents();
            
            base.OnDetach();
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
            
            var hitResult = this.Managers.PhysicManager3D.RayCast(ref ray, float.MaxValue, CollisionCategory3D.All);

            if (hitResult.Succeeded)
            {
                Debug.WriteLine($"Hit entity: {hitResult.Collider}, point: {hitResult.Point}, normal: {hitResult.Normal}");
            }
        }
        
        private void OnPointerPressed(object sender, PointerEventArgs data)
    {
        if (touchDispatcher.Points.Count == 1)
        {
            TryRaycastFromTouch();
            
            isRotating = true;
            lastPosition = data.Position.ToVector2();
        }
        else if (touchDispatcher.Points.Count == 2)
        {
            isRotating = false;
            isPanning = true;
            lastPinchDistance = GetPinchDistance();
        }
    }

    private void OnPointerMoved(object sender, PointerEventArgs data)
    {
        if (isRotating && lastPosition.HasValue)
        {
            var delta = data.Position.ToVector2() - lastPosition.Value;
            lastPosition = data.Position.ToVector2();
            RotateCamera(delta);
        }

        if (isPanning && touchDispatcher.Points.Count >= 2)
        {
            float currentDistance = GetPinchDistance();
            float zoomDelta = currentDistance - lastPinchDistance;
            lastPinchDistance = currentDistance;

            ZoomCamera(-zoomDelta);
        }
    }

    private void OnPointerReleased(object sender, PointerEventArgs data)
    {
            if (touchDispatcher.Points.Count <= 1)
            {
                isRotating = false;
                isPanning = false;
                lastPinchDistance = 0;
            }

        lastPosition = null;
    }

    private float GetPinchDistance()
    {
        var pointers = touchDispatcher.Points.ToArray();
        if (pointers.Length < 2) return 0;

        return Vector2.Distance(pointers[0].Position.ToVector2(), pointers[1].Position.ToVector2());
    }

    private void RotateCamera(Vector2 delta)
    {
        var yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -delta.X * RotationSpeed * 0.005f);
        var pitch = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -delta.Y * RotationSpeed * 0.005f);
        
        
        Transform.LocalOrientation = Quaternion.Normalize(pitch * yaw * Transform.LocalOrientation);
    }

    private void ZoomCamera(float delta)
    {
        Transform.LocalPosition += Transform.Forward * delta * ZoomSpeed * 0.1f;
    }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            this.RefreshDisplay();
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

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            this.graphicsPresenter.TryGetDisplay("DefaultDisplay", out var presenterDisplay);
            if (presenterDisplay != this.display) 
            {
                this.camera3D.DisplayTagDirty = true;
                this.RefreshDisplay();
            };

            this.HandleInput();

            if (this.isDirty)
            {
                this.isDirty = false;
            }
        }

        private void HandleInput()
        {
        }
    }
}
