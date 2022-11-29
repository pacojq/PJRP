using System.Runtime.CompilerServices;
using PJRP.Runtime.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace PJRP.Runtime.Core
{
    internal class Shadows
    {
        private static readonly int s_Id_DirectionalShadowAtlas = Shader.PropertyToID("_DirectionalShadowAtlas");
        private static readonly int s_Id_DirectionalShadowMatrices = Shader.PropertyToID("_DirectionalShadowMatrices");
        private static readonly int s_Id_CascadeCount = Shader.PropertyToID("_CascadeCount");
        private static readonly int s_Id_CascadeCullingSpheres = Shader.PropertyToID("_CascadeCullingSpheres");
        private static readonly int s_Id_CascadeData = Shader.PropertyToID("_CascadeData");
        private static readonly int s_Id_ShadowAtlasSize = Shader.PropertyToID("_ShadowAtlasSize");
        private static readonly int s_Id_ShadowDistanceFade = Shader.PropertyToID("_ShadowDistanceFade");
        
        private const int MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT = 4;
        private const int MAX_CASCADES = 4;
        
        private const string BUFFER_NAME = "Shadows";
        
        private static readonly string[] KEYWORDS_DIRECTIONAL_FILTER = new string[]
        {
            "_DIRECTIONAL_PCF3",
            "_DIRECTIONAL_PCF5",
            "_DIRECTIONAL_PCF7",
        };
        
        private static readonly string[] KEYWORDS_CASCADE_BLEND = new string[]
        {
            "_CASCADE_BLEND_SOFT",
            "_CASCADE_BLEND_DITHER"
        };
        
        private static readonly Vector4[] s_CascadeCullingSpheres  = new Vector4[MAX_CASCADES];
        private static readonly Vector4[] s_CascadeData  = new Vector4[MAX_CASCADES];
        private static readonly Matrix4x4[] s_DirShadowMatrices = new Matrix4x4[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADES];
        
        private struct ShadowedDirectionalLight
        {
            public int VisibleLightIndex;
            public float SlopeScaleBias;
            public float NearPlaneOffset;
        }



        private readonly ShadowedDirectionalLight[] _shadowedDirectionalLights;

        private readonly CommandBuffer _buffer;
        
        public Shadows()
        {
            _buffer = new CommandBuffer() 
            {
                name = BUFFER_NAME
            };
            
            _shadowedDirectionalLights = new ShadowedDirectionalLight[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT];
        }
        
        
        
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private ShadowSettings _settings;

        private int _shadowedDirectionalLightCount;
        
        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
        {
            this._context = context;
            this._cullingResults = cullingResults;
            this._settings = settings;

            _shadowedDirectionalLightCount = 0;
        }
        
        
        public void Render()
        {
            if (_shadowedDirectionalLightCount > 0)
            {
                RenderDirectionalShadows();
            }
            else
            {
                // Use a dummy 1x1 texture when no directional shadows are needed
                _buffer.GetTemporaryRT(s_Id_DirectionalShadowAtlas, 1, 1,
                        32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            }
        }

        private void RenderDirectionalShadows()
        {
            int atlasSize = (int) _settings.Directional.AtlasSize;

            // ==== PREPARE SHADOW ATLAS =========================================
            {
                _buffer.GetTemporaryRT(s_Id_DirectionalShadowAtlas, atlasSize, atlasSize,
                    32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

                _buffer.SetRenderTarget(s_Id_DirectionalShadowAtlas,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

                _buffer.ClearRenderTarget(true, false, Color.clear);
            }
            _buffer.BeginSample(BUFFER_NAME);
            ExecuteBuffer();

            // ==== RENDER SHADOWS FOR EACH LIGHT ================================
            {
                int tiles = MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * _settings.Directional.CascadeCount;
                int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
                int tileSize = atlasSize / split;

                for (int i = 0; i < _shadowedDirectionalLightCount; i++)
                {
                    RenderDirectionalShadows(i, split, tileSize);
                }
            }

            // ==== SET SHADER VARIABLES =========================================
            {
                float f = 1f - _settings.Directional.CascadeFade;
                _buffer.SetGlobalVector(s_Id_ShadowDistanceFade, new Vector4(
                    1f / _settings.MaxDistance, 
                    1f / _settings.DistanceFade,
                    1f / (1f - f * f)
                ));
                
                _buffer.SetGlobalInt(s_Id_CascadeCount, _settings.Directional.CascadeCount);
                _buffer.SetGlobalVectorArray(s_Id_CascadeCullingSpheres, s_CascadeCullingSpheres);
                _buffer.SetGlobalVectorArray(s_Id_CascadeData, s_CascadeData);
                _buffer.SetGlobalMatrixArray(s_Id_DirectionalShadowMatrices, s_DirShadowMatrices);
                
                SetKeywords(KEYWORDS_DIRECTIONAL_FILTER, (int)_settings.Directional.Filter - 1);
                SetKeywords(KEYWORDS_CASCADE_BLEND, (int)_settings.Directional.CascadeBlend - 1);
                
                _buffer.SetGlobalVector(s_Id_ShadowAtlasSize, new Vector4(atlasSize, 1f / atlasSize));
            }
            
            _buffer.EndSample(BUFFER_NAME);
            ExecuteBuffer();
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetKeywords(string[] keywords, int enabledIndex) 
        {
            for (int i = 0; i < keywords.Length; i++) 
            {
                if (i == enabledIndex) _buffer.EnableShaderKeyword(keywords[i]);
                else _buffer.DisableShaderKeyword(keywords[i]);
            }
        }
        
        
        
        
        
        
        private void RenderDirectionalShadows(int index, int split, int tileSize) 
        {
            ShadowedDirectionalLight light = _shadowedDirectionalLights[index];
            ShadowDrawingSettings shadowSettings = new ShadowDrawingSettings(_cullingResults, light.VisibleLightIndex);
            
            int cascadeCount = _settings.Directional.CascadeCount;
            int tileIndexOffset = index * cascadeCount;
            Vector3 ratios = _settings.Directional.CascadeRatios;
            
            float cullingFactor = Mathf.Max(0f, 0.8f - _settings.Directional.CascadeFade);

            for (int i = 0; i < cascadeCount; i++)
            {
                _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    light.VisibleLightIndex, i, cascadeCount, ratios, tileSize, 
                    light.NearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                    out ShadowSplitData splitData
                );
                splitData.shadowCascadeBlendCullingFactor = cullingFactor;
                shadowSettings.splitData = splitData;
                
                if (index == 0)
                {
                    SetCascadeData(i, splitData.cullingSphere, tileSize);
                }

                int tileIndex = tileIndexOffset + i;

                SetTileViewport(tileIndex, split, tileSize, out Vector2 tileOffset);
                s_DirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, tileOffset, split);
                
                
                _buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                _buffer.SetGlobalDepthBias(0f, light.SlopeScaleBias);
                ExecuteBuffer();
                _context.DrawShadows(ref shadowSettings);
                _buffer.SetGlobalDepthBias(0f, 0f);
            }
        }
        
        private void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
        {
            float texelSize = 2f * cullingSphere.w / tileSize;
            float filterSize = texelSize * ((float)_settings.Directional.Filter + 1f);
            
            // We need the spheres in the shader to check whether a surface fragment lies inside them, which
            // can be done by comparing the square distance from the sphere's center with its square radius.
            // So let's store the square radius instead, so we don't have to calculate it in the shader.
            cullingSphere.w -= filterSize;
            cullingSphere.w *= cullingSphere.w;
            s_CascadeCullingSpheres[index] = cullingSphere;

            const float SQRT_TWO = 1.4142136f;
            s_CascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * SQRT_TWO);
        }


        /// <summary>
        /// Prepare the viewport for a given light inside our target shadow render texture.
        /// </summary>
        /// <param name="index">The index of the directional light.</param>
        /// <param name="split">Which split the light is assigned to.</param>
        /// <param name="tileSize">The reserved size for each tile, depending on shadow atlas size.</param>
        /// <param name="offset">The calculated tile offset for this viewport.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetTileViewport(int index, int split, float tileSize, out Vector2 offset) 
        {
            offset = new Vector2(index % split, index / split);
            _buffer.SetViewport(new Rect(
                offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
            ));
        }
        
        /// <summary>
        /// Take a light matrix (`projection * view`) and return a matrix
        /// that converts from world space to shadow tile space.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="offset"></param>
        /// <param name="split"></param>
        /// <returns></returns>
        private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split) 
        {
            if (SystemInfo.usesReversedZBuffer) // Reverse Z Buffer in OpenGL renderers
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }
            
            // Convert from clip space's [-1, 1] space to a [0, 1] space.
            // To avoid needless operations, we directly modify the needed
            // elements inside the matrix.
            // We also have to take into account tile offset and scale, so
            // if the space transformation were to be:
            //
            //    m.m00 = 0.5f * (m.m00 + m.m30);
            //
            // we would need to change it to:
            //
            //     m.m00 = (0.5f * (m.m00 + m.m30) + splitOffset.x * m.m30) * splitScale;
            //
            float scale = 1f / split;
            
            m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
            m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
            m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
            m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
            m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
            m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
            m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
            m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
            
            m.m20 = 0.5f * (m.m20 + m.m30);
            m.m21 = 0.5f * (m.m21 + m.m31);
            m.m22 = 0.5f * (m.m22 + m.m32);
            m.m23 = 0.5f * (m.m23 + m.m33);
            
            return m;
        }
        
        
        

        /// <summary>
        /// Reserves an index for a given light, so it can be later rendered to the shadow atlas.
        /// </summary>
        /// <param name="light"></param>
        /// <param name="visibleLightIndex"></param>
        /// <param name="shadowData">Packed: shadow strength (x component), shadow tile offset (y comp.) and shadow normal bias (z comp.).</param>
        public void ReserveDirectionalShadows(Light light, int visibleLightIndex, out Vector3 shadowData)
        {
            if (_shadowedDirectionalLightCount < MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT
                    && light.shadows != LightShadows.None && light.shadowStrength > 0f
                    && _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
            {
                _shadowedDirectionalLights[_shadowedDirectionalLightCount] = new ShadowedDirectionalLight() 
                {
                    VisibleLightIndex = visibleLightIndex,
                    SlopeScaleBias = light.shadowBias,
                    NearPlaneOffset = light.shadowNearPlane
                };
                shadowData = new Vector3(
                    light.shadowStrength, 
                    _settings.Directional.CascadeCount * _shadowedDirectionalLightCount++,
                    light.shadowNormalBias
                );
                return;
            }
            
            shadowData = Vector3.zero;
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cleanup() 
        {
            _buffer.ReleaseTemporaryRT(s_Id_DirectionalShadowAtlas);
            ExecuteBuffer();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteBuffer()
        {
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }
    }
}