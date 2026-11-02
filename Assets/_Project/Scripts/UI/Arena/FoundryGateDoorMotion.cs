using System.Collections.Generic;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // Borrows the region-owned facade batch mesh and swings the gate's two leaves outward on their jamb hinges while the
    // floor's way out is the gate: from the boss's fall (the gate exit opening) until the next floor begins. Structure
    // depth stays at each vertex's authored value; only the leaves' positions move.
    public sealed class FoundryGateDoorMotion : MonoBehaviour
    {
        // Fully open the leaves stand this far round from the wall, and they take this long to get there.
        public const float OpenAngleDegrees = 72f, SwingSeconds = 1.4f;
        private Mesh _mesh;
        private Vector3[] _rest, _working;
        private FoundryDoorVertex[] _leaves;
        private float _face;
        private CombatSetup _setup;
        private bool _searched;
        public float Opening { get; private set; }

        internal void Initialize(Mesh mesh,List<FoundryDoorVertex> leaves,float face)
        {
            _mesh=mesh;_rest=mesh.vertices;_working=(Vector3[])_rest.Clone();_leaves=leaves.ToArray();_face=face;
            _mesh.MarkDynamic();
        }

        private bool ShouldBeOpen()
        {
            if(!_searched){_setup=FindFirstObjectByType<CombatSetup>();_searched=true;}
            var encounter=_setup!=null?_setup.Encounters:null;
            return encounter!=null&&(encounter.IsGateExitOpen||encounter.IsFloorCleared);
        }

        private void Update()
        {
            if(_mesh==null)return;
            float target=ShouldBeOpen()?1f:0f;
            if(target<Opening)Opening=0f; // A new floor shuts the gate at once; it only ever swings open.
            else if(target>Opening)Opening=Mathf.Min(1f,Opening+Time.deltaTime/SwingSeconds);
            else if(Opening==0f)return;
            Pose(Opening);
        }

        internal void Pose(float opening)
        {
            float eased=opening*opening*(3f-2f*opening);
            float angle=eased*OpenAngleDegrees*Mathf.Deg2Rad,cos=Mathf.Cos(angle),sin=Mathf.Sin(angle);
            foreach(var leaf in _leaves)
            {
                // Shut is exactly the built painting, not a recomputed approximation of it.
                if(eased<=0f){_working[leaf.Index]=_rest[leaf.Index];continue;}
                // Outward is -u, onto the landing; the leaf's free edge closes toward its hinge along the wall.
                Vector3 point=FoundrySurface.Project(_face-Mathf.Abs(leaf.Reach)*sin,leaf.Hinge+leaf.Reach*cos)+Vector3.up*leaf.Height;
                point.z=_rest[leaf.Index].z;
                _working[leaf.Index]=point;
            }
            _mesh.SetVertices(_working,0,_working.Length,UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
        }

        private void OnDisable()
        {
            if(_mesh!=null&&_rest!=null)_mesh.SetVertices(_rest,0,_rest.Length,UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
        }
    }
}
