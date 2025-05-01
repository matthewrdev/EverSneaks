using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Evergine.Common.Graphics;
using Evergine.Common.IO;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Graphics.Materials;
using Evergine.Framework.Runtimes;
using Evergine.Framework.Services;
using EverSneaks.Services;
using Random = Evergine.Framework.Services.Random;

namespace EverSneaks
{
    public partial class MyApplication : Application
    {
        MyScene scene;
        
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
            scene = assetsService.Load<MyScene>(EvergineContent.Scenes.MyScene_wescene);
            ScreenContext screenContext = new ScreenContext(scene);
            screenContextManager.To(screenContext);
        }

        public async Task LoadGlb(string filePath)
        {
            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            var assetsDirectory = Application.Current.Container.Resolve<AssetsDirectory>();
            
            Model model = null;
            using (var fileStream = File.OpenRead(filePath))
            {
                model = await Evergine.Runtimes.GLB.GLBRuntime.Instance.Read(fileStream, CustomMaterialAssigner);
            }
            
            var entity = model.InstantiateModelHierarchy(assetsService);
            var screenContextManager = this.Container.Resolve<ScreenContextManager>();

            entity.Name = "Runtime loaded GLB";
            
            scene.Managers.EntityManager.Add(entity);
        }
        
        private async Task<Material> CustomMaterialAssigner(MaterialData data)
{
    var assetsService = Application.Current.Container.Resolve<AssetsService>();

    // Get textures            
    var baseColor = await data.GetBaseColorTextureAndSampler();
    var metallicRoughness = await data.GetMetallicRoughnessTextureAndSampler();
    var normalTex = await data.GetNormalTextureAndSampler();  
    var emissive = await data.GetEmissiveTextureAndSampler();
    var occlussion = await data.GetOcclusionTextureAndSampler();            

    // Get Layer
    var opaqueLayer = assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.OpaqueRenderLayerID);
    var alphaLayer = assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.AlphaRenderLayerID);
    RenderLayerDescription layer;
    float alpha = data.BaseColor.A / 255.0f;
    switch (data.AlphaMode)
    {
        default:
        case Evergine.Framework.Runtimes.AlphaMode.Mask:
        case Evergine.Framework.Runtimes.AlphaMode.Opaque:
            layer = opaqueLayer;
            break;
        case Evergine.Framework.Runtimes.AlphaMode.Blend:
            layer = alphaLayer;
            break;
    }

    // Create standard material            
    var effect = assetsService.Load<Effect>(DefaultResourcesIDs.StandardEffectID);            
    StandardMaterial standard = new StandardMaterial(effect)
    {
        LightingEnabled = data.HasVertexNormal,
        IBLEnabled = data.HasVertexNormal,
        BaseColor = data.BaseColor,
        Alpha = alpha,
        BaseColorTexture = baseColor.Texture,
        BaseColorSampler = baseColor.Sampler,
        Metallic = data.MetallicFactor,
        Roughness = data.RoughnessFactor,
        MetallicRoughnessTexture = metallicRoughness.Texture,
        MetallicRoughnessSampler = metallicRoughness.Sampler,
        EmissiveColor = data.EmissiveColor.ToColor(),
        EmissiveTexture = emissive.Texture,
        EmissiveSampler = emissive.Sampler,
        OcclusionTexture = occlussion.Texture,
        OcclusionSampler = occlussion.Sampler,
        LayerDescription = layer,                
    };

    // Normal textures
    if (data.HasVertexTangent)
    {
        standard.NormalTexture = normalTex.Texture;
        standard.NormalSampler = normalTex.Sampler;
    }

    // Alpha test
    if (data.AlphaMode == Evergine.Framework.Runtimes.AlphaMode.Mask)
    {
        standard.AlphaCutout = data.AlphaCutoff;
    }

    // Vertex Color
    if (data.HasVertexColor)
    {
        if (standard.ActiveDirectivesNames.Contains("VCOLOR"))
        {
            var directivesArray = standard.ActiveDirectivesNames;
            Array.Resize(ref directivesArray, directivesArray.Length + 1);
            directivesArray[directivesArray.Length - 1] = "VCOLOR";
            standard.ActiveDirectivesNames = directivesArray;
        }
    }

    return standard.Material;
}
    }
}


