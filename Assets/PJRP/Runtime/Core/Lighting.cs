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
        
        private const int MAX_DIR_LIGHT_COUNT = 4;
        private static readonly Vector4[] s_DirLightColors = new Vector4[MAX_DIR_LIGHT_COUNT];
        private static readonly Vector4[] s_DirLightDirections = new Vector4[MAX_DIR_LIGHT_COUNT];
        private static readonly Vector4[] s_DirLightShadowData = new Vector4[MAX_DIR_LIGHT_COUNT];
        
        private const int MAX_OTHER_LIGHT_COUNT = 64;
        private static readonly Vector4[] s_OtherLightColors = new Vector4[MAX_OTHER_LIGHT_COUNT];
        private static readonly Vector4[] s_OtherLightPositions = new Vector4[MAX_OTHER_LIGHT_COUNT];
        
        
        
        
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
	
        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
        {
            _cullingResults = cullingResults;
            
            _buffer.BeginSample(BUFFER_NAME);
            {
                _shadows.Setup(context, _cullingResults, shadowSettings);
                SetupLights();

                _shadows.Render();
            }
            _buffer.EndSample(BUFFER_NAME);
            context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }



        private void SetupLights()
        {
            NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;
            
            int dirLightCount = 0;
            int otherLightCount = 0;
            
            for (int i = 0; i < visibleLights.Length; i++)
            {
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
                            SetupPointLight(otherLightCount++, ref visibleLight);
                        break;
                    }
                }
            }
            
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
        }
        
        
        
        
        
        public void Cleanup() 
        {
            _shadows.Cleanup();
        }
    }
}