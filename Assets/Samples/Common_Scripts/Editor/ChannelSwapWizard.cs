using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Samples.Common_Scripts.Editor
{
    public class ChannelSwapWizard : ScriptableWizard
    {
        public enum Channel
        {
            R,
            G,
            B,
            A
        }
        
        public enum Operation
        {
            Copy,
            Invert
        }

        [MenuItem("PJRP/Tools/Swap Texture Channels...")]
        public static void OpenWizard()
        {
            const string title = "Swap Texture Channels";
            ChannelSwapWizard wiz = DisplayWizard<ChannelSwapWizard>(title, "Execute");
        }

        [SerializeField] private Texture2D _texture;
        [SerializeField, Range(1, 4)] private int _outChannelCount = 4;
        
        [Header("Red Channel")]
        [SerializeField] private Channel _outRed = Channel.R;
        [SerializeField] private Operation _opRed;
        
        [Header("Red Channel")]
        [SerializeField] private Channel _outGreen = Channel.G;
        [SerializeField] private Operation _opGreen;
        
        [Header("Red Channel")]
        [SerializeField] private Channel _outBlue = Channel.B;
        [SerializeField] private Operation _opBlue;
        
        [Header("Alpha Channel")]
        [SerializeField] private Channel _outAlpha = Channel.A;
        [SerializeField] private Operation _opAlpha;


        private void OnEnable()
        {
            OnValidate();
        }


        private void OnValidate()
        {
            if (_texture == null)
            {
                this.isValid = false;
                this.errorString = "Please, select a texture to modify.";
            }
            else
            {
                this.isValid = true;
                this.errorString = string.Empty;
            }
        }

        private void OnWizardCreate()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Texture", "Swapped Texture", "png", "Save Texture", "Assets");
            if (string.IsNullOrEmpty(path))
                return;
            
            Color[] pixels = _texture.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color src = pixels[i];
                Color res = pixels[i];

                res.r = GetChannel(src, _outRed, _opRed);
                res.g = GetChannel(src, _outGreen, _opGreen);
                res.b = GetChannel(src, _outBlue, _opBlue);
                res.a = GetChannel(src, _outAlpha, _opAlpha);

                pixels[i] = res;
            }

            Texture2D tex = new Texture2D(_texture.width, _texture.height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.SetPixels(pixels);
            tex.Apply();
            
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            DestroyImmediate(tex);
            AssetDatabase.Refresh();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetChannel(Color c, Channel channel, Operation op)
        {
            float v = c.a;
            
            if (channel == Channel.R) v = c.r;
            else if (channel == Channel.G) v = c.g;
            else if (channel == Channel.B) v = c.b;

            if (op == Operation.Invert) return 1 - v;
            return v;
        }
    }
}