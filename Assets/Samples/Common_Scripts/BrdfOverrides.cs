using System;
using UnityEngine;

namespace Samples.Common_Scripts
{
    [ExecuteAlways]
    public class BrdfOverrides : MonoBehaviour
    {
        private static readonly int s_Id_Metallic = Shader.PropertyToID("_Metallic");
        private static readonly int s_Id_Smoothness = Shader.PropertyToID("_Smoothness");
        
        [SerializeField, Range(0, 1)] private float _metallic;
        [SerializeField, Range(0, 1)] private float _smoothness;

        private void OnValidate()
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            
            block.SetFloat(s_Id_Metallic, _metallic);
            block.SetFloat(s_Id_Smoothness, _smoothness);
            
            GetComponent<Renderer>().SetPropertyBlock(block);
        }
    }
}