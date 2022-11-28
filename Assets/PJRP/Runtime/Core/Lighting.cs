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
        
        private const int MAX_DIR_LIGHT_COUNT = 4;
        private static readonly Vector4[] s_DirLightColors = new Vector4[MAX_DIR_LIGHT_COUNT];
        private static readonly Vector4[] s_DirLightDirections = new Vector4[MAX_DIR_LIGHT_COUNT];
        
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
            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];

                if (visibleLight.lightType == LightType.Directional)
                {
                    SetupDirectionalLight(dirLightCount++, ref visibleLight);
                    if (dirLightCount >= MAX_DIR_LIGHT_COUNT)
                        break;
                }
            }
            
            _buffer.SetGlobalInt(s_Id_DirectionalLightCount, visibleLights.Length);
            _buffer.SetGlobalVectorArray(s_Id_DirectionalLightColors, s_DirLightColors);
            _buffer.SetGlobalVectorArray(s_Id_DirectionalLightDirections, s_DirLightDirections);
        }
        
        
        
        private void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
        {
            s_DirLightColors[index] = visibleLight.finalColor;
            s_DirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2); // Equivalent to: -lightTransform.forward
            
            _shadows.ReserveDirectionalShadows(visibleLight.light, index);
        }
        
        public void Cleanup() 
        {
            _shadows.Cleanup();
        }
    }
}