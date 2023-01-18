using PJRP.Runtime.Core;
using PJRP.Runtime.Settings;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;

using LightType = UnityEngine.LightType;


namespace PJRP.Runtime
{
    public partial class PJRenderPipeline : RenderPipeline
    {
        partial void InitializeForEditor();
        
#if UNITY_EDITOR

        /// <summary>
        /// Configures a LightDataGI for each light found
        /// in the scene, so we can properly bake them.
        /// </summary>
        static void CustomRequestLightsDelegate(Light[] lights, NativeArray<LightDataGI> output)
        {
            var lightData = new LightDataGI();
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                switch (light.type)
                {
                    case LightType.Directional:
                        var directionalLight = new DirectionalLight();
                        LightmapperUtils.Extract(light, ref directionalLight);
                        lightData.Init(ref directionalLight);
                        break;
                    case LightType.Point:
                        var pointLight = new PointLight();
                        LightmapperUtils.Extract(light, ref pointLight);
                        lightData.Init(ref pointLight);
                        break;
                    case LightType.Spot:
                        var spotLight = new SpotLight();
                        LightmapperUtils.Extract(light, ref spotLight);
                        spotLight.innerConeAngle = light.innerSpotAngle * Mathf.Deg2Rad;
                        spotLight.angularFalloff = AngularFalloffType.AnalyticAndInnerAngle;
                        lightData.Init(ref spotLight);
                        break;
                    case LightType.Area:
                        var rectangleLight = new RectangleLight();
                        LightmapperUtils.Extract(light, ref rectangleLight);
                        rectangleLight.mode = LightMode.Baked; // Realtime area lights are not supported
                        lightData.Init(ref rectangleLight);
                        break;
                    
                    default:
                        lightData.InitNoBake(light.GetInstanceID());
                        break;
                }
                lightData.falloff = FalloffType.InverseSquared;
                output[i] = lightData;
            }
        }
        
        partial void InitializeForEditor()
        {
            Lightmapping.SetDelegate(CustomRequestLightsDelegate);
        }
        
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Lightmapping.ResetDelegate();
        }

#endif
    }
}