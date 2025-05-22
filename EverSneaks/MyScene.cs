using System.Linq;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;

namespace EverSneaks
{
    public class DefaultScene : Scene
    {
        public override void RegisterManagers()
        {
            base.RegisterManagers();
            
            this.Managers.AddManager(new global::Evergine.Bullet.BulletPhysicManager3D());
            
            // var defaultCameraEntity = this.Managers.EntityManager.FindAllByTag("DefaultCamera").FirstOrDefault();
            // if (defaultCameraEntity != null)
            // {
            //     this.Managers.EntityManager.Remove(defaultCameraEntity);
            // }
            
            
        }
        
        // private void CreateFreeCamera()
        // {
        //     var cameraEntity = new Entity("FreeCamera");
        //
        //     // Add a transform (position and orientation)
        //     var transform = new Transform3D
        //     {
        //         LocalPosition = new Vector3(0, 1.8f, 5),
        //         LocalRotation = Quaternion.ToEuler(Quaternion.Identity)
        //     };
        //     cameraEntity.AddComponent(transform);
        //
        //     // Add the actual camera
        //     cameraEntity.AddComponent(new Camera3D());
        //
        //     // Optionally add a free movement script
        //     cameraEntity.AddComponent(new FreeCameraBehavior
        //     {
        //         MovementSpeed = 5.0f,
        //         RotationSpeed = 1.0f
        //     });
        //
        //     // Set as active camera
        //     this.Managers.CameraManager.SetActiveCamera(cameraEntity);
        // }

        protected override void CreateScene()
        {            
        }
    }
    
}


