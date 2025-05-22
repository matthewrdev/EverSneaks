using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using EverSneaks.Components;
using System.Linq;

namespace EverSneaks.Services
{
    public class ControllerService : Service
    {
        private MaterialComponent materialComponent;
        private CameraBehavior cameraBehavior;

        protected override void Start()
        {
            base.Start();

            //  Select material component
            var screenContextManager = Application.Current.Container.Resolve<ScreenContextManager>();
            screenContextManager.OnActivatingScene += (scene) =>
            {
                var entity = scene.Managers.EntityManager.FindAllByTag("SneakersMesh").First();
                this.materialComponent = entity.FindComponent<MaterialComponent>();
                
                this.cameraBehavior = scene.Managers.EntityManager.FindComponentsOfType<CameraBehavior>().First();
            };
        }
    }
}
