using UnityEngine;
using UnityEngine.Rendering;

namespace PJRP.Runtime.Core
{
    internal partial class CameraRenderer
    {
        private static readonly ShaderTagId s_ShaderTag_Unlit = new ShaderTagId("SRPDefaultUnlit");
        private static readonly ShaderTagId s_ShaderTag_Lit = new ShaderTagId("PJRPLit");
        
        private const string BUFFER_NAME = "[PJRP] Render Camera";

        private readonly CommandBuffer _buffer;
        private readonly Lighting _lighting;
        
        public CameraRenderer()
        {
            _buffer = new CommandBuffer()
            {
                name = BUFFER_NAME
            };
            
            _lighting = new Lighting();
        }
        
        
        
        
        private ScriptableRenderContext _context;
        private Camera _camera;
        
        private CullingResults _cullingResults;
        
        
        public void Render(ScriptableRenderContext context, Camera camera, PJRenderPipeline rp)
        {
            this._context = context;
            this._camera = camera;

            Editor_PrepareBuffer();
            Editor_PrepareForSceneWindow();
            
            if (!Cull())
                return;
            
            SetUp();
            {
                _lighting.Setup(context, _cullingResults);
                
                DrawVisibleGeometry(rp.UseDynamicBatching, rp.UseGPUInstancing);
                
                Editor_DrawUnsupportedShaders();
                Editor_DrawGizmos();
            }
            Submit();
        }



        private bool Cull()
        {
            if (!_camera.TryGetCullingParameters(out ScriptableCullingParameters cullParams))
                return false;

            _cullingResults = _context.Cull(ref cullParams);
            
            return true;
        }
        
        
        
        
        private void SetUp()
        {
            _context.SetupCameraProperties(_camera);
            
            CameraClearFlags clearFlags = _camera.clearFlags;
            _buffer.ClearRenderTarget(
                clearFlags <= CameraClearFlags.Depth, 
                clearFlags == CameraClearFlags.Color, 
                clearFlags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear
                );
            
            _buffer.BeginSample(SampleName);
            ExecuteCommandBuffer();
        }
        
        
        
        private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
        {
            // ===== DRAW OPAQUES ================================================================
            
            SortingSettings sortingSettings = new SortingSettings(_camera);
            DrawingSettings drawingSettings = new DrawingSettings(s_ShaderTag_Unlit, sortingSettings)
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing
            };
            drawingSettings.SetShaderPassName(1, s_ShaderTag_Lit);
            
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            _context.DrawRenderers(
                _cullingResults, ref drawingSettings, ref filteringSettings
            );
            
            // ===== DRAW SKYBOX =================================================================
            
            _context.DrawSkybox(_camera);
            
            
            // ===== DRAW TRANSPARENTS ===========================================================
            
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;

            _context.DrawRenderers(
                _cullingResults, ref drawingSettings, ref filteringSettings
            );
        }
        
        
        
        
        
        private void ExecuteCommandBuffer()
        {
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }

        private void Submit()
        {
            _buffer.EndSample(SampleName);
            ExecuteCommandBuffer();
            _context.Submit();
        }
    }
}