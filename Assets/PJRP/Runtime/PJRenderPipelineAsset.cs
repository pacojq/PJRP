using System.Collections;
using System.Collections.Generic;
using PJRP.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace PJRP.Runtime
{
    [CreateAssetMenu(menuName = "Rendering/PJRP/Custom Render Pipeline")]
    public class PJRenderPipelineAsset : RenderPipelineAsset
    {
        [SerializeField] public bool UseDynamicBatching = true;
        [SerializeField] public bool UseGPUInstancing = true;
        [SerializeField] public bool UseSRPBatcher = true;
        
        [SerializeField] public ShadowSettings Shadows = default;
        
        protected override RenderPipeline CreatePipeline()
        {
            return new PJRenderPipeline(this);
        }
    }
}
