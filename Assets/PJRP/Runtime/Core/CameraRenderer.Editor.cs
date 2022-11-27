using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PJRP.Runtime.Core
{
    internal partial class CameraRenderer
    {
#if UNITY_EDITOR
        private static readonly ShaderTagId[] s_LegacyShaderTagIds = 
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM")
        };
        private static Material s_ErrorMaterial;
#endif

        
#if !UNITY_EDITOR
	    const string SampleName = BUFFER_NAME;
#else
        string SampleName { get; set; }
#endif
        
        


        partial void Editor_PrepareBuffer();
        
#if UNITY_EDITOR
        partial void Editor_PrepareBuffer()
        {
            Profiler.BeginSample("Editor Only");
            _buffer.name = SampleName = string.Concat("[PJRP] ", _camera.name);
            Profiler.EndSample();
        }
#endif
        
        
        
        
        partial void Editor_PrepareForSceneWindow();
        
#if UNITY_EDITOR
        partial void Editor_PrepareForSceneWindow()
        {
            if (_camera.cameraType == CameraType.SceneView)
            {
                // Explicitly add the UI to the world geometry when rendering for the scene window
                ScriptableRenderContext.EmitWorldGeometryForSceneView(_camera);
            }
        }
#endif
        
        

        partial void Editor_DrawUnsupportedShaders();
        
#if UNITY_EDITOR
        partial void Editor_DrawUnsupportedShaders()
        {
            if (s_ErrorMaterial == null)
            {
                s_ErrorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }
            
            DrawingSettings drawingSettings = new DrawingSettings(s_LegacyShaderTagIds[0], new SortingSettings(_camera))
            {
                overrideMaterial = s_ErrorMaterial
            };
            drawingSettings.SetShaderPassName(1, s_ShaderTag_Lit);
            for (int i = 1; i < s_LegacyShaderTagIds.Length; i++)
                drawingSettings.SetShaderPassName(i, s_LegacyShaderTagIds[i]);
            
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;
            
            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }
#endif

        partial void Editor_DrawGizmos();
        
#if UNITY_EDITOR
        partial void Editor_DrawGizmos()
        {
            if (Handles.ShouldRenderGizmos())
            {
                _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
                _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
            }
        }
#endif
    }
}