using PJRP.Runtime.Settings;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace PJRP.Runtime.Core
{
    internal class Lighting
    {
        private static readonly int s_Id_DirectionalLightCount = Shader.PropertyToID("_DirectionalLightCount");
        private static readonly int s_Id_DirectionalLightColors = Shader.PropertyToID("_DirectionalLightColors");
        private static readonly int s_Id_DirectionalLightDirections = Shader.PropertyToID("_DirectionalLightDirections");
        private static readonly int s_Id_DirectionalLightShadowData = Shader.PropertyToID("_DirectionalLightShadowData");
        
        private static readonly int s_Id_OtherLightCount = Shader.PropertyToID("_OtherLightCount");
        private static readonly int s_Id_OtherLightColors = Shader.PropertyToID("_OtherLightColors");
        private static readonly int s_Id_OtherLightPositions = Shader.PropertyToID("_OtherLightPositions");
        private static readonly int s_Id_OtherLightDirections = Shader.PropertyToID("_OtherLightDirections");
        private static readonly int s_Id_OtherLightSpotAngles = Shader.PropertyToID("_OtherLightSpotAngles"); // Control how wide spot light cones are
        private static readonly int s_Id_OtherLightShadowData = Shader.PropertyToID("_OtherLightShadowData");
        
        private const int MAX_DIR_LIGHT_COUNT = 4;
        private static readonly Vector4[] s_DirLightColors = new Vector4[MAX_DIR_LIGHT_COUNT];
        private static readonly Vector4[] s_DirLightDirections = new Vector4[MAX_DIR_LIGHT_COUNT];
        private static readonly Vector4[] s_DirLightShadowData = new Vector4[MAX_DIR_LIGHT_COUNT];
        
        private const int MAX_OTHER_LIGHT_COUNT = 64;
        private static readonly Vector4[] s_OtherLightColors = new Vector4[MAX_OTHER_LIGHT_COUNT];
        private static readonly Vector4[] s_OtherLightPositions = new Vector4[MAX_OTHER_LIGHT_COUNT];
        private static readonly Vector4[] s_OtherLightDirections = new Vector4[MAX_OTHER_LIGHT_COUNT];
        private static readonly Vector4[] s_OtherLightSpotAngles = new Vector4[MAX_OTHER_LIGHT_COUNT];
        private static readonly Vector4[] s_OtherLightShadowData = new Vector4[MAX_OTHER_LIGHT_COUNT];
        
        private const string KEYWORD_LIGHTS_PER_OBJECT = "_LIGHTS_PER_OBJECT";
        
        
        private const string BUFFER_NAME = "Lighting";

        
        private readonly CommandBuffer _buffer;
        private readonly Shadows _shadows;

        public Lighting()
        {
            _buffer = new CommandBuffer()
            {
                name = BUFFER_NAME
            };
            
            _shadows = new Shadows();
        }
        
        
        private CullingResults _cullingResults;
	
        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings,
                bool useLightsPerObject)
        {
            _cullingResults = cullingResults;
            
            _buffer.BeginSample(BUFFER_NAME);
            {
                _shadows.Setup(context, _cullingResults, shadowSettings);
                SetupLights(useLightsPerObject);

                _shadows.Render();
            }
            _buffer.EndSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }



        private void SetupLights(bool useLightsPerObject)
        {
            NativeArray<int> indexMap = useLightsPerObject
                ? _cullingResults.GetLightIndexMap(Allocator.Temp) // Fetch visible light indices
                : default;
            NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;
            
            int dirLightCount = 0;
            int otherLightCount = 0;
            
            int i;
            for (i = 0; i < visibleLights.Length; i++)
            {
                int newIndex = -1;
                VisibleLight visibleLight = visibleLights[i];

                switch (visibleLight.lightType)
                {
                    case LightType.Directional:
                    {
                        if (dirLightCount < MAX_DIR_LIGHT_COUNT)
                            SetupDirectionalLight(dirLightCount++, ref visibleLight);
                        break;
                    }
                    case LightType.Point:
                    {
                        if (otherLightCount < MAX_OTHER_LIGHT_COUNT)
                        {
                            newIndex = otherLightCount;
                            SetupPointLight(otherLightCount++, ref visibleLight);
                        }
                        break;
                    }
                    case LightType.Spot:
                    {
                        if (otherLightCount < MAX_OTHER_LIGHT_COUNT)
                        {
                            newIndex = otherLightCount;
                            SetupSpotLight(otherLightCount++, ref visibleLight);
                        }
                        break;
                    }
                }
                
                if (useLightsPerObject) indexMap[i] = newIndex;
            }
            
            if (useLightsPerObject) // Clean up excess in visible light indices
            {
                for (; i < indexMap.Length; i++)
                    indexMap[i] = -1;
                
                _cullingResults.SetLightIndexMap(indexMap);
                indexMap.Dispose();
                
                Shader.EnableKeyword(KEYWORD_LIGHTS_PER_OBJECT);
            }
            else Shader.DisableKeyword(KEYWORD_LIGHTS_PER_OBJECT);
            

            _buffer.SetGlobalInt(s_Id_DirectionalLightCount, dirLightCount);
            if (dirLightCount > 0)
            {
                _buffer.SetGlobalVectorArray(s_Id_DirectionalLightColors, s_DirLightColors);
                _buffer.SetGlobalVectorArray(s_Id_DirectionalLightDirections, s_DirLightDirections);
                _buffer.SetGlobalVectorArray(s_Id_DirectionalLightShadowData, s_DirLightShadowData);
            }
            
            _buffer.SetGlobalInt(s_Id_OtherLightCount, otherLightCount);
            if (otherLightCount > 0)
            {
                _buffer.SetGlobalVectorArray(s_Id_OtherLightColors, s_OtherLightColors);
                _buffer.SetGlobalVectorArray(s_Id_OtherLightPositions, s_OtherLightPositions);
                _buffer.SetGlobalVectorArray(s_Id_OtherLightDirections, s_OtherLightDirections);
                _buffer.SetGlobalVectorArray(s_Id_OtherLightSpotAngles, s_OtherLightSpotAngles);
                _buffer.SetGlobalVectorArray(s_Id_OtherLightShadowData, s_OtherLightShadowData);
            }
        }
        
        
        
        private void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
        {
            s_DirLightColors[index] = visibleLight.finalColor;
            s_DirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2); // Equivalent to: -lightTransform.forward
            
            _shadows.ReserveDirectionalShadows(visibleLight.light, index, out Vector4 shadowData);
            s_DirLightShadowData[index] = shadowData;
        }
        
        private void SetupPointLight(int index, ref VisibleLight visibleLight)
        {
            s_OtherLightColors[index] = visibleLight.finalColor;
            
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f); // Range-dependent attenuation stored in 'w' component of position vector
            s_OtherLightPositions[index] = position;
            
            s_OtherLightSpotAngles[index] = new Vector4(0f, 1f);
            
            _shadows.ReserveOtherShadows(visibleLight.light, index, out Vector4 shadowData);
            s_OtherLightShadowData[index] = shadowData;
        }
        
        private void SetupSpotLight(int index, ref VisibleLight visibleLight)
        {
            s_OtherLightColors[index] = visibleLight.finalColor;
            
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f); // Range-dependent attenuation stored in 'w' component of position vector
            s_OtherLightPositions[index] = position;
            
            s_OtherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2); // Equivalent to: -lightTransform.forward
            
            Light light = visibleLight.light;
            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
            float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
            s_OtherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv); // Store inner and outer spot light cone angles
            
            _shadows.ReserveOtherShadows(light, index, out Vector4 shadowData);
            s_OtherLightShadowData[index] = shadowData;
        }
        
        
        
        
        
        public void Cleanup() 
        {
            _shadows.Cleanup();
        }
    }
}