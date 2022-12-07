using System;
using UnityEngine;

namespace Samples.Common_Scripts
{
    public class LightProbeGroupGenerator : MonoBehaviour
    {
        [SerializeField] private LightProbeGroup _group;
        [SerializeField] private Vector3Int _size;
        [SerializeField] private float _spacing;

        

        private void OnValidate()
        {
            if (_group == null)
                return;

            _size.x = Mathf.Max(1, _size.x);
            _size.y = Mathf.Max(1, _size.y);
            _size.z = Mathf.Max(1, _size.z);
            _spacing = Mathf.Max(0.1f, _spacing);

            Vector3[] positions = new Vector3[_size.x * _size.y * _size.z];

            int index = 0;
            Vector3 pos = Vector3.zero;
            for (int z = 0; z < _size.z; z++)
            {
                pos.z = -(_spacing * _size.z) * .5f + (_spacing * z);
                for (int y = 0; y < _size.y; y++)
                {
                    pos.y = -(_spacing * _size.y) * .5f + (_spacing * y);
                    for (int x = 0; x < _size.x; x++)
                    {
                        pos.x = -(_spacing * _size.x) * .5f + (_spacing * x);

                        positions[index++] = pos;
                    }
                }
            }


            _group.probePositions = positions;
            // TODO
        }
    }
}