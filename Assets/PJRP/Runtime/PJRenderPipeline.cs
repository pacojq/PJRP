using PJRP.Runtime.Core;
using PJRP.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace PJRP.Runtime
{
    public class PJRenderPipeline : RenderPipeline
    {
        public bool UseDynamicBatching => _useDynamicBatching;
        private readonly bool _useDynamicBatching;
        
        public bool UseGPUInstancing => _useGPUInstancing;
        private readonly bool _useGPUInstancing;

        public ShadowSettings Shadows => _shadows;
        private readonly ShadowSettings _shadows;
        
        
        private readonly PJRenderPipelineAsset _asset;
        private readonly CameraRenderer _renderer;


        internal PJRenderPipeline(PJRenderPipelineAsset asset)
        {
            _asset = asset;
            _renderer = new CameraRenderer();
            
            this._useDynamicBatching = asset.UseDynamicBatching;
            this._useGPUInstancing = asset.UseGPUInstancing;
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.UseSRPBatcher;

            this._shadows = asset.Shadows;
        }
        
        
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        { 
            for (int i = 0; i < cameras.Length; i++)
            {
                _renderer.Render(context, cameras[i], this);
            }
        }
    }
}