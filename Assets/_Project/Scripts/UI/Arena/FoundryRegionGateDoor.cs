using UnityEngine;

namespace Cryptforge.UI
{
    internal readonly struct FoundryDoorVertex
    {
        internal readonly int Index;
        // The jamb the vertex's leaf hangs on (design v), its signed distance from that hinge along the wall (toward the
        // seam) and its height above the landing.
        internal readonly float Hinge, Reach, Height;
        internal FoundryDoorVertex(int index,float hinge,float reach,float height){Index=index;Hinge=hinge;Reach=reach;Height=height;}
    }

    public sealed partial class FoundryRegionView
    {
        // The painted door's two leaves (GateFacade texture x .3385-.5 and .5-.6641, y .0303-.8828), re-laid over the
        // painting exactly, so nothing changes until the floor's boss falls. They then swing outward onto the landing on
        // their jamb hinges (FoundryGateDoorMotion), framing the gate exit, over a doorway lit from the forge hall beyond.
        // Outward is also the one way the leaves can draw over the facade without passing through the wall. Same
        // material, rank and depth structure as the facade, so the inner piers and every actor keep their order.
        internal const float GateDoorLeftU=.3385f,GateDoorSeamU=.5f,GateDoorRightU=.6641f,GateDoorBottomV=.0303f,GateDoorTopV=.8828f;
        private static float GateDoorWallV(float textureX)=>11f-12f*textureX;
        // The facade's wall plane (design u), the leaves' shut position and the hinge line they swing from.
        internal static float GateDoorFaceU=>GateU(31.99f);
        // SceneryOrder maps this to the facade's (-12) sorting order, but as a separate batch key.
        private const float GateDoorOrder=-11.75f;

        private void BuildGateDoorway(Material facade,float height)
        {
            float face=GateDoorFaceU;
            float left=GateDoorWallV(GateDoorLeftU),seam=GateDoorWallV(GateDoorSeamU),right=GateDoorWallV(GateDoorRightU);
            float bottom=GateDoorBottomV*height,top=GateDoorTopV*height;
            var door=new Builder();
            // The doorway reads as the forge hall beyond: a warm lit floor receding from the threshold, the glow of a
            // furnace mouth on the far wall at its centre, and darkness under the vault. One flat patch of the door's
            // brass bar carries vertex colour on a small grid across the opening.
            var flat=new Vector2(.4844f,.789f);
            float[] across={0f,.16f,.32f,.5f,.74f,1f},up={0f,.18f,.34f,.52f,.76f,1f};
            int first=door.V.Count;
            for(int row=0;row<up.Length;row++)
                for(int column=0;column<across.Length;column++)
                {
                    float v=Mathf.Lerp(left,right,across[column]),h=Mathf.Lerp(bottom,top,up[row]);
                    door.V.Add(Project(face,v)+Vector3.up*h);door.U.Add(flat);door.C.Add(HallLight(across[column],up[row]));
                }
            for(int row=0;row+1<up.Length;row++)
                for(int column=0;column+1<across.Length;column++)
                {
                    int a0=first+row*across.Length+column,b0=a0+1,c0=a0+across.Length+1,d0=a0+across.Length;
                    door.T.AddRange(new[]{a0,b0,c0,a0,c0,d0});
                }
            // Two leaves, each on its own jamb: left (wall v left..seam) and right (seam..right).
            DoorLeaf(door,face,left,seam,GateDoorLeftU,GateDoorSeamU,bottom,top);
            DoorLeaf(door,face,right,seam,GateDoorRightU,GateDoorSeamU,bottom,top);
            // Its own batch, so no other motion (the facade batch's flames) ever rewrites the leaves' vertices; it shares
            // the facade's sorting order and sorts just after it (FoundryRegionView.Emit).
            Create("Gate door leaves",door,facade,GateDoorOrder);
        }

        // Light through the doorway at (across 0..1, up 0..1): the hall floor near the threshold, the far furnace's
        // glow in the middle, the vault dark above. Below LightCeiling so the floor-decal clamp never bites.
        internal static Color HallLight(float across,float up)
        {
            // The open right leaf hides the opening's right half from the camera, so the far furnace stands left of
            // centre, where it is seen.
            float centre=1f-Mathf.Clamp01(Mathf.Abs(across-.32f)/.42f);
            float floor=Mathf.Clamp01(1f-up/.34f);
            float furnace=Mathf.Clamp01(1f-Mathf.Abs(up-.34f)/.30f)*centre;
            float vault=Mathf.Clamp01((up-.55f)/.45f);
            float r=.30f+1.05f*floor+1.25f*furnace;
            float g=.14f+.58f*floor+.70f*furnace;
            float b=.08f+.30f*floor+.26f*furnace;
            float dark=1f-.8f*vault;
            return new Color(r*dark,g*dark,b*dark,1);
        }

        private static void DoorLeaf(Builder door,float face,float hinge,float seam,float hingeX,float seamX,float bottom,float top)
        {
            int first=door.V.Count;
            float reach=seam-hinge;
            foreach(var (along,height,x,y) in new[]{(0f,bottom,hingeX,GateDoorBottomV),(reach,bottom,seamX,GateDoorBottomV),
                (reach,top,seamX,GateDoorTopV),(0f,top,hingeX,GateDoorTopV)})
            {
                door.Door.Add(new FoundryDoorVertex(door.V.Count,hinge,along,height));
                door.V.Add(Project(face,hinge+along)+Vector3.up*height);
                door.U.Add(new Vector2(x,y));door.C.Add(Color.white);
            }
            door.T.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
        }
    }
}
