using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Samples.Common_Scripts.Editor
{
    public class TextureMixWizard : ScriptableWizard
    {
        [MenuItem("PJRP/Tools/Mix Textures...")]
        public static void OpenWizard()
        {
            const string title = "Mix Textures";
            TextureMixWizard wiz = DisplayWizard<TextureMixWizard>(title, "Bake");
        }

        [SerializeField, Min(32)] private int _resolution = 1024;
        
        [SerializeField] private Texture2D _textureRed;
        [SerializeField] private Texture2D _textureGreen;
        [SerializeField] private Texture2D _textureBlue;
        [SerializeField] private Texture2D _textureAlpha;
        

        private void OnWizardCreate()
        {
            if (ExtractPath(out string path) == false)
                return;
            
            Color[] result = new Color[_resolution * _resolution];
            Vector2 uv = Vector2.zero;

            int index = 0;
            for (int y = 0; y < _resolution; y++)
            {
                uv.y = y / (float)(_resolution - 1);
                
                for (int x = 0; x < _resolution; x++)
                {
                    uv.x = x / (float)(_resolution - 1);
                    
                    Color res = result[index];

                    res.r = SampleTexture(_textureRed, uv);
                    res.g = SampleTexture(_textureGreen, uv);
                    res.b = SampleTexture(_textureBlue, uv);
                    res.a = SampleTexture(_textureAlpha, uv);

                    result[index++] = res;
                }
            }

            Texture2D tex = new Texture2D(_resolution, _resolution, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.SetPixels(result);
            tex.Apply();
            
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            DestroyImmediate(tex);
            AssetDatabase.Refresh();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float SampleTexture(Texture2D t, Vector2 uv)
        {
            if (t == null) return 1f;
            return t.GetPixelBilinear(uv.x, uv.y).r;
        }
        
        
        private bool ExtractPath(out string path)
        {
            string folder = null;
            string name = null;

            if (_textureRed != null)
            {
                folder = AssetDatabase.GetAssetPath(_textureRed);
                name = _textureRed.name;
            }
            else if (_textureGreen != null)
            {
                folder = AssetDatabase.GetAssetPath(_textureGreen);
                name = _textureGreen.name;
            }
            else if (_textureBlue != null)
            {
                folder = AssetDatabase.GetAssetPath(_textureBlue);
                name = _textureBlue.name;
            }
            else if (_textureAlpha != null)
            {
                folder = AssetDatabase.GetAssetPath(_textureAlpha);
                name = _textureAlpha.name;
            }

            if (folder == null) folder = "Assets";
            else folder = Path.GetDirectoryName(folder);

            if (name == null) name = "Bake Texture";
            
            path = EditorUtility.SaveFilePanelInProject("Save Texture", name, "png", "Save Texture",folder);
            if (string.IsNullOrEmpty(path))
                return false;
            return true;
        }
    }
}