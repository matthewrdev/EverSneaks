using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Evergine.Common.Graphics;
using Evergine.Common.IO;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Graphics.Materials;
using Evergine.Framework.Managers;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Runtimes;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using EverSneaks.Services;
using Random = Evergine.Framework.Services.Random;

namespace EverSneaks
{
    public partial class MyApplication : Application
    {
        
    readonly Vector3 AssetScale =new Vector3(40,40,40);
    private readonly Quaternion AssetRotation = Quaternion.Identity;
    
        DefaultScene scene;
        
        public MyApplication()
        {
            this.Container.Register<Settings>();
            this.Container.Register<Clock>();
            this.Container.Register<TimerFactory>();
            this.Container.Register<Random>();
            this.Container.Register<ErrorHandler>();
            this.Container.Register<ScreenContextManager>();
            this.Container.Register<GraphicsPresenter>();
            this.Container.Register<AssetsDirectory>();
            this.Container.Register<AssetsService>();
            this.Container.Register<ForegroundTaskSchedulerService>();
            this.Container.Register<WorkActionScheduler>();
            this.Container.Register<ControllerService>();
        }

        public override void Initialize()
        {
            base.Initialize();

            // Get ScreenContextManager
            var screenContextManager = this.Container.Resolve<ScreenContextManager>();
            var assetsService = this.Container.Resolve<AssetsService>();

            // Navigate to scene
            scene = assetsService.Load<DefaultScene>(EvergineContent.Scenes.MyScene_wescene);
            ScreenContext screenContext = new ScreenContext(scene);
            screenContextManager.To(screenContext);
        }

        public async Task LoadGlb(string filePath)
        {
            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            
            Model model = null;
            using (var fileStream = File.OpenRead(filePath))
            {
                // model = await Evergine.Runtimes.GLB.GLBRuntime.Instance.Read(fileStream);
                model = await Evergine.Runtimes.GLB.GLBRuntime.Instance.Read(fileStream, CustomMaterialAssigner);
            }
            
            var entity = model.InstantiateModelHierarchy(assetsService);
            var root = new Entity().AddComponent(new Transform3D());
            root.AddChild(entity);

            var collider = new MeshCollider3D()
            {
                IsConvex = false,
            };
            
            root.AddComponent(collider);
            root.AddComponent(new StaticBody3D());
            
            var transform = root.FindComponent<Transform3D>();
            if (transform != null)
            {
                transform.LocalScale = AssetScale;
                transform.LocalRotation = Quaternion.ToEuler(AssetRotation);
            }

            ((RenderManager)this.scene.Managers.RenderManager).DebugLines = true;
            
            // Add to scene
            scene.Managers.EntityManager.Add(root);
            
            RouteSceneLoader.LoadSceneFromJson(scene, this, Color.WhiteSmoke);
        }
        
        // Only Diffuse channel is needed
        private async Task<Material> CustomMaterialAssigner(MaterialData data)
        {
            var assetsService = Application.Current.Container.Resolve<AssetsService>();

            // Get textures            
            var baseColor = await data.GetBaseColorTextureAndSampler();

            // Get Layer
            var opaqueLayer = assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.OpaqueRenderLayerID);
            var alphaLayer = assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.AlphaRenderLayerID);
            RenderLayerDescription layer;
            float alpha = data.BaseColor.A / 255.0f;
            switch (data.AlphaMode)
            {
                default:
                case AlphaMode.Mask:
                case AlphaMode.Opaque:
                    layer = opaqueLayer;
                    break;
                case AlphaMode.Blend:
                    layer = alphaLayer;
                    break;
            }

            // Create standard material            
            var effect = assetsService.Load<Effect>(DefaultResourcesIDs.StandardEffectID);
            StandardMaterial standard = new StandardMaterial(effect)
            {
                BaseColor = data.BaseColor,
                Alpha = alpha,
                BaseColorTexture = baseColor.Texture,
                BaseColorSampler = baseColor.Sampler,
                Metallic = data.MetallicFactor,
                Roughness = data.RoughnessFactor,
                EmissiveColor = data.EmissiveColor.ToColor(),                
                LayerDescription = layer,
            };

            // Alpha test
            if (data.AlphaMode == AlphaMode.Mask)
            {
                standard.AlphaCutout = data.AlphaCutoff;
            }            

            return standard.Material;
        }
    }
}


