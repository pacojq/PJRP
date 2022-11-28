using PJRP.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace PJRP.Runtime.Core
{
    internal class Shadows
    {
        private static int s_Id_DirectionalShadowAtlas = Shader.PropertyToID("_DirectionalShadowAtlas");
        
        private const int MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT = 1;
        
        private const string BUFFER_NAME = "Shadows";
        
        private struct ShadowedDirectionalLight
        {
            public int visibleLightIndex;
        }



        private readonly ShadowedDirectionalLight[] _shadowedDirectionalLights;

        private readonly CommandBuffer _buffer;
        
        public Shadows()
        {
            _buffer = new CommandBuffer() 
            {
                name = BUFFER_NAME
            };
            
            _shadowedDirectionalLights = new ShadowedDirectionalLight[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT];
        }
        
        
        
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private ShadowSettings _settings;

        private int _shadowedDirectionalLightCount;
        
        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
        {
            this._context = context;
            this._cullingResults = cullingResults;
            this._settings = settings;

            _shadowedDirectionalLightCount = 0;
        }
        
        
        public void Render()
        {
            if (_shadowedDirectionalLightCount > 0)
            {
                RenderDirectionalShadows();
            }
            else
            {
                // Use a dummy 1x1 texture when no directional shadows are needed
                _buffer.GetTemporaryRT(s_Id_DirectionalShadowAtlas, 1, 1,
                        32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            }
        }

        private void RenderDirectionalShadows()
        {
            int atlasSize = (int) _settings.Directional.AtlasSize;
            
            _buffer.GetTemporaryRT(s_Id_DirectionalShadowAtlas, atlasSize, atlasSize,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            
            _buffer.SetRenderTarget(s_Id_DirectionalShadowAtlas,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            
            _buffer.ClearRenderTarget(true, false, Color.clear);
            ExecuteBuffer();
        }
        
        
        

        public void ReserveDirectionalShadows(Light light, int visibleLightIndex)
        {
            if (_shadowedDirectionalLightCount < MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT
                    && light.shadows != LightShadows.None && light.shadowStrength > 0f
                    && _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
            {
                _shadowedDirectionalLights[_shadowedDirectionalLightCount++] = new ShadowedDirectionalLight() 
                {
                    visibleLightIndex = visibleLightIndex
                };
            }
        }
        
        
        public void Cleanup() 
        {
            _buffer.ReleaseTemporaryRT(s_Id_DirectionalShadowAtlas);
            ExecuteBuffer();
        }
        
        private void ExecuteBuffer()
        {
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }
    }
}