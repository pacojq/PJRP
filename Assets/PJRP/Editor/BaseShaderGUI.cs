using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PJRP.Editor
{
    public abstract class BaseShaderGUI : ShaderGUI
    {
        private MaterialEditor _editor;
        private Object[] _materials;
        private MaterialProperty[] _properties;
        
        
        public sealed override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);
            
            _editor = materialEditor;
            _materials = materialEditor.targets;
            _properties = properties;

            EditorGUILayout.Space();
            
            OnMaterialGUI();
        }

        protected abstract void OnMaterialGUI();
        
        
        

        protected void SetRenderQueue(RenderQueue queue)
        {
            for (int i = 0; i < _materials.Length; i++)
            {
                Material m = (Material) _materials[i];
                m.renderQueue = (int) queue;
            }
        }

        protected bool HasProperty(string name)
        {
            return FindProperty(name, _properties, false) != null;
        }

        protected bool SetProperty(string name, float value)
        {
            MaterialProperty property = FindProperty(name, _properties, false);
            if (property != null)
            {
                property.floatValue = value;
                return true;
            }
            return false;
        }
        
        protected void SetProperty(string name, string keyword, bool value)
        {
            if (SetProperty(name, value ? 1f : 0f))
                SetKeyword(keyword, value);
        }
        
        protected void SetKeyword(string keyword, bool enabled)
        {
            if (enabled)
            {
                for (int i = 0; i < _materials.Length; i++)
                {
                    Material m = (Material) _materials[i];
                    m.EnableKeyword(keyword);
                }
            }
            else
            {
                for (int i = 0; i < _materials.Length; i++)
                {
                    Material m = (Material) _materials[i];
                    m.DisableKeyword(keyword);
                }
            }
        }
        
        
        protected bool PresetButton(string name)
        {
            if (GUILayout.Button(name))
            {
                _editor.RegisterPropertyChangeUndo(name);
                return true;
            }
            return false;
        }
    }
}