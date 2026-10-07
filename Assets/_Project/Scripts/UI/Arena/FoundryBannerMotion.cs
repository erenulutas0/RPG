using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.UI
{
    internal readonly struct FoundryBannerVertex
    {
        internal readonly int Index;
        internal readonly float Drop, Phase;
        internal FoundryBannerVertex(int index, float drop, float phase)
        { Index=index; Drop=drop; Phase=phase; }
    }

    // Borrows the facade batch. Identical cloth coordinates use identical phases,
    // so duplicated quad edges remain welded visually. The hanging rod never moves.
    public sealed class FoundryBannerMotion : MonoBehaviour
    {
        private Mesh _mesh;
        private Vector3[] _rest, _working;
        private FoundryBannerVertex[] _cloth;
        private float _elapsed, _nextFrame;

        internal void Initialize(Mesh mesh, List<FoundryBannerVertex> cloth)
        {
            _mesh=mesh; _rest=mesh.vertices; _working=(Vector3[])_rest.Clone(); _cloth=cloth.ToArray();
            mesh.MarkDynamic();
            var bounds=mesh.bounds; bounds.Expand(.3f); mesh.bounds=bounds;
        }

        private void Update()
        {
            if(_mesh==null || Time.deltaTime<=0)return;
            _elapsed+=Time.deltaTime;
            if(_elapsed<_nextFrame)return;
            _nextFrame=_elapsed+1f/30;
            foreach(var cloth in _cloth)
            {
                float weight=cloth.Drop*cloth.Drop;
                float phase=_elapsed*1.8f+cloth.Phase-cloth.Drop*2.8f;
                Vector3 point=_rest[cloth.Index];
                point.x+=weight*(.09f*Mathf.Sin(phase)+.025f*Mathf.Sin(phase*1.7f));
                point.y+=weight*.025f*Mathf.Sin(phase+.8f);
                _working[cloth.Index]=point;
            }
            _mesh.SetVertices(_working,0,_working.Length,UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
        }

        private void OnDisable()
        {
            if(_mesh!=null && _rest!=null)
                _mesh.SetVertices(_rest,0,_rest.Length,UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
        }
    }
}
