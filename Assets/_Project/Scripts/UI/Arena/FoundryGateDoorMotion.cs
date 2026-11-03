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

        // Every place a leaf vertex takes on its swing, as a box on screen, for the gate's depth cover (FoundryDepthCover).
        // Opening, the vertex's design u only falls and its v only moves one way (Pose), and FoundrySurface.Project is
        // monotone in u and in v on both screen axes, so the corners of the design box the arc spans bound it on screen.
        internal static void Sweep(FoundryDoorVertex leaf,float face,out Vector2 min,out Vector2 max)
        {
            float angle=OpenAngleDegrees*Mathf.Deg2Rad;
            float open=face-Mathf.Abs(leaf.Reach)*Mathf.Sin(angle);
            float shutV=leaf.Hinge+leaf.Reach,openV=leaf.Hinge+leaf.Reach*Mathf.Cos(angle);
            Vector3 lift=Vector3.up*leaf.Height;
            Vector3 a=FoundrySurface.Project(face,shutV)+lift,b=FoundrySurface.Project(face,openV)+lift;
            Vector3 c=FoundrySurface.Project(open,shutV)+lift,d=FoundrySurface.Project(open,openV)+lift;
            min=new Vector2(Mathf.Min(Mathf.Min(a.x,b.x),Mathf.Min(c.x,d.x)),Mathf.Min(Mathf.Min(a.y,b.y),Mathf.Min(c.y,d.y)));
            max=new Vector2(Mathf.Max(Mathf.Max(a.x,b.x),Mathf.Max(c.x,d.x)),Mathf.Max(Mathf.Max(a.y,b.y),Mathf.Max(c.y,d.y)));
        }

        private void OnDisable()
        {
            if(_mesh!=null&&_rest!=null)_mesh.SetVertices(_rest,0,_rest.Length,UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
        }
    }
}
