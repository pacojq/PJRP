using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PJRP.Editor
{
    public class PJRPShaderGUI : BaseShaderGUI
    {
        bool Clipping
        {
            set => SetProperty("_Clipping", "_CLIPPING", value);
        }

        bool PremultiplyAlpha
        {
            set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
        }

        BlendMode SrcBlend
        {
            set => SetProperty("_SrcBlend", (float)value);
        }

        BlendMode DstBlend
        {
            set => SetProperty("_DstBlend", (float)value);
        }

        bool ZWrite
        {
            set => SetProperty("_ZWrite", value ? 1f : 0f);
        }

        RenderQueue RenderQueue
        {
            set => SetRenderQueue(value);
        }
        
        enum ShadowMode 
        {
            On, Clip, Dither, Off
        }

        ShadowMode Shadows 
        {
            set 
            {
                if (SetProperty("_Shadows", (float)value)) 
                {
                    SetKeyword("_SHADOWS_CLIP", value == ShadowMode.Clip);
                    SetKeyword("_SHADOWS_DITHER", value == ShadowMode.Dither);
                }
            }
        }
        
        private bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");
        
        
        private bool _showPresets;


        protected override void OnMaterialGUI()
        {
            EditorGUI.BeginChangeCheck();
            
            _showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true);
            if (_showPresets)
            {
                OpaquePreset();
                ClipPreset();
                FadePreset();
                TransparentPreset();
            }
            
            if (EditorGUI.EndChangeCheck())
                SetShadowCasterPass();
        }
        
        protected override void OnBaseMaterialGUIChangeCheck()
        {
            SetShadowCasterPass();
        }
        


        private void OpaquePreset()
        {
            if (PresetButton("Opaque"))
            {
                Clipping = false;
                PremultiplyAlpha = false;
                SrcBlend = BlendMode.One;
                DstBlend = BlendMode.Zero;
                ZWrite = true;
                RenderQueue = RenderQueue.Geometry;
                Shadows = ShadowMode.On;
            }
        }
        
        private void ClipPreset()
        {
            if (PresetButton("Clip"))
            {
                Clipping = true;
                PremultiplyAlpha = false;
                SrcBlend = BlendMode.One;
                DstBlend = BlendMode.Zero;
                ZWrite = true;
                RenderQueue = RenderQueue.AlphaTest;
                Shadows = ShadowMode.Clip;
            }
        }
        
        private void FadePreset() // Standard transparency.
        {
            if (PresetButton("Fade"))
            {
                Clipping = false;
                PremultiplyAlpha = false;
                SrcBlend = BlendMode.SrcAlpha;
                DstBlend = BlendMode.OneMinusSrcAlpha;
                ZWrite = false;
                RenderQueue = RenderQueue.Transparent;
                Shadows = ShadowMode.Dither;
            }
        }
        
        private void TransparentPreset() // Semitransparent surfaces with correct lighting. Uses premultiplied alpha.
        {
            if (HasPremultiplyAlpha && PresetButton("Transparent"))
            {
                Clipping = false;
                PremultiplyAlpha = true;
                SrcBlend = BlendMode.One;
                DstBlend = BlendMode.OneMinusSrcAlpha;
                ZWrite = false;
                RenderQueue = RenderQueue.Transparent;
                Shadows = ShadowMode.Dither;
            }
        }
        
        private void SetShadowCasterPass()
        {
            MaterialProperty shadows = FindProperty("_Shadows");
            if (shadows == null || shadows.hasMixedValue)
                return;
            
            bool enabled = shadows.floatValue < (float)ShadowMode.Off;
            SetShaderPassEnabled("ShadowCaster", enabled);
        }
    }
}