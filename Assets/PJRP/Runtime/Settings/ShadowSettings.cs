using System;
using UnityEngine;

namespace PJRP.Runtime.Settings
{
    [Serializable]
    public class ShadowSettings
    {
        [SerializeField, Min(0.0f)] public float MaxDistance = 100f;


        [SerializeField] public DirectionalSettings Directional = new DirectionalSettings() 
        {
            AtlasSize = TextureSize._1024
        };
        
        
        public enum TextureSize
        {
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048, 
            _4096 = 4096, 
            _8192 = 8192
        }

        
        [System.Serializable]
        public struct DirectionalSettings
        {
            public TextureSize AtlasSize;
        }
        
    }
}