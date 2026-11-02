using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.UI
{
    internal readonly struct FoundryFlameVertex
    {
        internal readonly int Index;
        internal readonly float Weight, Phase;
        internal FoundryFlameVertex(int index,float weight,float phase)
        { Index=index;Weight=weight;Phase=phase; }
    }

    // Borrows the region-owned batch mesh. Only tagged open-flame tips move; stone,
    // coal beds, window panes and structure depth remain at their authored positions.
    public sealed class FoundryFlameMotion : MonoBehaviour
    {
        private Mesh _mesh;
        private Vector3[] _rest, _working;
        private FoundryFlameVertex[] _tips;
        private float _elapsed, _nextFrame;

        internal void Initialize(Mesh mesh,List<FoundryFlameVertex> tips)
        {
            _mesh=mesh;_rest=mesh.vertices;_working=(Vector3[])_rest.Clone();_tips=tips.ToArray();
            _mesh.MarkDynamic();
            var bounds=mesh.bounds;bounds.Expand(.4f);mesh.bounds=bounds;
        }

        private void Update()
        {
            if(_mesh==null||Time.deltaTime<=0)return;
            _elapsed+=Time.deltaTime;
            if(_elapsed<_nextFrame)return;
            _nextFrame=_elapsed+1f/30;
            foreach(var tip in _tips)
            {
                Vector3 point=_rest[tip.Index];
                float sway=Mathf.Sin(_elapsed*5.1f+tip.Phase)*.055f+
                    Mathf.Sin(_elapsed*8.3f+tip.Phase*1.7f)*.025f;
                float rise=Mathf.Sin(_elapsed*6.7f+tip.Phase*.8f)*.055f;
                point.x+=sway*tip.Weight;point.y+=rise*tip.Weight;
                _working[tip.Index]=point;
            }
            // Reuse arrays; no per-frame mesh reads, allocations or bounds recalculation.
            _mesh.SetVertices(_working,0,_working.Length,UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
        }

        private void OnDisable()
        {
            if(_mesh!=null&&_rest!=null)_mesh.SetVertices(_rest,0,_rest.Length,UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
        }
    }
}
