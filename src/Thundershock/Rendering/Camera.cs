using System;
using System.Numerics;
using Thundershock.Core;

namespace Thundershock.Rendering
{
    public class CameraManager
    {
        private Scene _scene;
        private Camera _camera;
        
        public CameraManager(Scene scene)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            
            // TODO: ECS.
            _camera = new Camera(this);
        }

        public Rectangle ViewportBounds => _scene.Graphics.ViewportBounds;

        public Camera ActiveCamera
        {
            get => _camera;
            set => _camera = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    public enum CameraProjectionType
    {
        Perspective,
        Orthographic
    }
    
    public class Camera
    {
        private CameraManager _manager;

        public Camera(CameraManager cameraManager)
        {
            _manager = cameraManager;
        }

        public Transform Transform { get; } = new();
        
        public float OrthoRight { get; set; } = 1920;
        public float OrthoBottom { get; set; } = 1080;
        public float PerspectiveWidth { get; set; } = 800;
        public float PerspectiveHeight { get; set; } = 800;
        public float ZFar { get; set; } = 1000;
        public float ZNear { get; set; } = 0;

        public float PerspectiveNearDistance { get; set; } = 1f;
        public float PerspectiveFarDistance { get; set; } = 100;
        
        public CameraProjectionType ProjectionType { get; set; } = CameraProjectionType.Orthographic;
        public AspectRatioMode AspectRatioMode { get; set; } = AspectRatioMode.ScaleHorizontally;
        public Camera()
        {
        }

        public Matrix4x4 ProjectionMatrix
        {
            get => GetProjectionMatrixInternal();
        }

        private Matrix4x4 GetProjectionMatrixInternal()
        {
            var result = Matrix4x4.Identity;
            var aspect = _manager.ViewportBounds.Width / _manager.ViewportBounds.Height;


            var transform = Transform.GetTransformMatrix();

            Matrix4x4.Invert(transform, out result);
            result *= Matrix4x4.CreateWorld(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
            result *= Matrix4x4.CreateLookAt(Vector3.UnitZ, Vector3.Zero, -Vector3.UnitY);
            
            switch (ProjectionType)
            {
                case CameraProjectionType.Perspective:
                    result *= Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4.0f, aspect, PerspectiveNearDistance,
                       PerspectiveFarDistance);
                    // result *= Matrix4x4.CreatePerspective(_manager.ViewportBounds.Width, _manager.ViewportBounds.Height, PerspectiveNearDistance, PerspectiveFarDistance);
                    break;
                case CameraProjectionType.Orthographic:
                    var width = OrthoRight;
                    var height = OrthoBottom;

                    if (AspectRatioMode == AspectRatioMode.ScaleVertically)
                        height = width * aspect;
                    else if (AspectRatioMode == AspectRatioMode.ScaleHorizontally)
                        width = height * aspect;
                    
                    result *= Matrix4x4.CreateOrthographic(width, height, ZNear, ZFar);
                    break;
            }


            return result;
        }
    }
}