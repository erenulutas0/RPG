using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Core;
using Cryptforge.UI;
using Cryptforge.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // Static art-layout inspection only. No navigation or game geometry is replaced.
    public sealed class FoundryPerimeterCapture
    {
        private const string Output="ArtDirection/2026-09-23/foundry-perimeter-01/";
        private Camera _camera;
        private Scene _scene;
        private readonly List<Mesh> _owned=new List<Mesh>();
        private readonly HashSet<Vector2Int> _cells=new HashSet<Vector2Int>();
        private readonly int[,] _rects={{-4,-20,4,-10},{-10,-10,10,12},{-22,-6,-12,12},{4,10,12,22},{0,20,16,30},{-14,-6,-8,0},{-14,6,-8,12}};
        private static Vector3 Project(float x,float y) => new Vector3((x-y)*.70710678f,(x+y)*.35355339f,0);

        [UnityTest] public IEnumerator RenderApprovedLayout()
        {
            Directory.CreateDirectory(Output);TestProfile.Begin();
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _scene=SceneManager.GetActiveScene();Time.timeScale=0;
            var arena=Object.FindFirstObjectByType<ArenaView>();
            var hero=Object.FindFirstObjectByType<HeroLookView>();
            Object.FindFirstObjectByType<CombatSetup>().Encounters.enabled=false;
            hero.GetComponent<HeroMovementInput>().enabled=false;
            foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if(!r.transform.IsChildOf(hero.transform))r.enabled=false;
            foreach(var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))c.enabled=false;
            _camera=Camera.main;_camera.GetComponent<ArenaCameraFollow>().enabled=false;
            _camera.clearFlags=CameraClearFlags.SolidColor;_camera.backgroundColor=new Color(.055f,.067f,.105f);
            for(int r=0;r<_rects.GetLength(0);r++)for(int x=_rects[r,0];x<_rects[r,2];x++)for(int y=_rects[r,1];y<_rects[r,3];y++)_cells.Add(new Vector2Int(x,y));
            // Verify the proposed union is connected, including the optional two-link loop.
            var visited=new HashSet<Vector2Int>();var queue=new Queue<Vector2Int>();queue.Enqueue(new Vector2Int(0,-15));
            var dirs=new[]{Vector2Int.right,Vector2Int.up,Vector2Int.left,Vector2Int.down};
            while(queue.Count>0){var p=queue.Dequeue();if(!visited.Add(p))continue;foreach(var d in dirs)if(_cells.Contains(p+d)&&!visited.Contains(p+d))queue.Enqueue(p+d);}
            Assert.That(visited.Count,Is.EqualTo(_cells.Count));Assert.That(_cells.Contains(new Vector2Int(-11,3)),Is.False,"Side recess stays open in the visual plan.");
            var floor=new Builder();var walls=new Builder();var trim=new Builder();
            foreach(var cell in _cells)
            {
                float x=cell.x,y=cell.y;
                var p=new[]{Project(x,y),Project(x+1,y),Project(x+1,y+1),Project(x,y+1)};
                floor.Quad(p[0],p[1],p[2],p[3],true,Color.white);
                var neighbours=new[]{Vector2Int.down,Vector2Int.right,Vector2Int.up,Vector2Int.left};
                for(int i=0;i<4;i++)if(!_cells.Contains(cell+neighbours[i]))
                {
                    Vector3 a=p[i],b=p[(i+1)%4],drop=Vector3.down*.8f;
                    walls.Quad(a+drop,b+drop,b,a,false,new Color(.7f,.7f,.76f));
                    Vector3 inward=(Project(x+.5f,y+.5f)-(a+b)*.5f)*.18f;
                    trim.Quad(a,b,b+inward,a+inward,false,new Color(.7f,.65f,.54f));
                }
            }
            Mesh surface=Create("Layout surface",floor,arena.PlatformMaterials.Floor,-15);
            Create("Layout sidewalls",walls,arena.PlatformMaterials.Wall,-16);
            Create("Layout coping",trim,arena.PlatformMaterials.Coping,-14);
            BuildPerimeter(arena);
            hero.gameObject.SetActive(false);
            var bounds=surface.bounds;_camera.aspect=1600f/1100;
            _camera.orthographicSize=Mathf.Max(bounds.size.y*.5f,bounds.size.x/(_camera.aspect*2))*1.12f;
            _camera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-10);Capture("overview",1600,1100);
            hero.gameObject.SetActive(true);
            int index=0;
            foreach(var location in new[]{new Vector2(0,-15),new Vector2(0,0),new Vector2(-17,3),new Vector2(8,16),new Vector2(8,25)})
            {
                hero.transform.position=Project(location.x,location.y);
                _camera.aspect=1080f/2340;_camera.orthographicSize=9f/(2*_camera.aspect);
                _camera.transform.position=hero.transform.position+new Vector3(0,1,-10);
                Capture("zone-"+index++,1080,2340);
            }
            File.WriteAllText(Output+"measurement.txt","Connected unit cells: "+_cells.Count+"\nStatic view width: 9\nNo navigation, collision, HUD, combat or device verification.\n");
            LogAssert.NoUnexpectedReceived();
        }
        private void BuildPerimeter(ArenaView arena)
        {
            var furnace=UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Props/Foundry/Furnace.tga");
            Assert.That(furnace,Is.Not.Null);
            // Visual annexes only: deliberately excluded from the proposed walking cells.
            foreach(float y in new[]{14f,19f})
            {
                Block(arena,12,y,15,y+3,0,.9f,"Furnace foundation");
                var obj=new GameObject("Supported furnace");
                obj.transform.position=Project(13.5f,y+1.5f);obj.transform.localScale=Vector3.one*.72f;
                var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=furnace;renderer.sortingOrder=-12;
            }
            // Low solid buttresses articulate the perimeter without occupying the fighting square.
            foreach(float y in new[]{-8f,0f,8f})
                Block(arena,10,y,11.5f,y+1.5f,.45f,.8f,"Square edge buttress");
            foreach(float y in new[]{-4f,3f,10f})
                Block(arena,-23.5f,y,-22,y+1.5f,.45f,.8f,"Salvage edge buttress");
        }
        private void Block(ArenaView arena,float x0,float y0,float x1,float y1,float rise,float depth,string name)
        {
            Vector3 lift=Vector3.up*rise;
            var p=new[]{Project(x0,y0)+lift,Project(x1,y0)+lift,Project(x1,y1)+lift,Project(x0,y1)+lift};
            var top=new Builder();top.Quad(p[0],p[1],p[2],p[3],true,new Color(.7f,.7f,.75f));
            var side=new Builder();var raised=new Builder();
            for(int i=0;i<4;i++)
            {
                Vector3 a=p[i]-lift,b=p[(i+1)%4]-lift;
                side.Quad(a-Vector3.up*depth,b-Vector3.up*depth,b,a,false,new Color(.65f,.65f,.72f));
                if(rise>0)raised.Quad(a,b,p[(i+1)%4],p[i],false,new Color(.65f,.65f,.72f));
            }
            // Below-floor masonry must not paint over the adjacent walking surface.
            Create(name+" foundation",side,arena.PlatformMaterials.Wall,-16);
            if(rise>0)Create(name+" raised sides",raised,arena.PlatformMaterials.Wall,-14);
            Create(name+" cap",top,arena.PlatformMaterials.Floor,rise>0?-13:-15);
        }
        private Mesh Create(string name,Builder b,Material material,int order)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(b.V);mesh.SetUVs(0,b.U);mesh.SetColors(b.C);mesh.SetTriangles(b.T,0);mesh.RecalculateBounds();_owned.Add(mesh);
            var obj=new GameObject(name);obj.AddComponent<MeshFilter>().sharedMesh=mesh;var r=obj.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingOrder=order;return mesh;
        }
        private sealed class Builder
        {
            internal readonly List<Vector3> V=new List<Vector3>();internal readonly List<Vector2> U=new List<Vector2>();
            internal readonly List<Color> C=new List<Color>();internal readonly List<int> T=new List<int>();
            internal void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool floor,Color color)
            {
                int n=V.Count;var points=new[]{a,b,c,d};var uv=new[]{Vector2.zero,new Vector2(.25f,0),new Vector2(.25f,1),Vector2.up};
                for(int i=0;i<4;i++){V.Add(points[i]);C.Add(color);U.Add(floor?new Vector2(1+(points[i].x+points[i].y*2)/8.55f,1+(-points[i].x+points[i].y*2)/8.55f):uv[i]);}
                T.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
        }
        private void Capture(string name,int width,int height)
        {
            var target=RenderTexture.GetTemporary(width,height,24);var previous=RenderTexture.active;
            float aspect=_camera.aspect;_camera.aspect=(float)width/height;_camera.targetTexture=target;
            _camera.Render();RenderTexture.active=target;
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            using(var writer=new BinaryWriter(File.Create(Output+name+".bmp")))
            {
                int bytes=width*height*3;writer.Write((ushort)0x4d42);writer.Write(bytes+54);writer.Write(0);writer.Write(54);
                writer.Write(40);writer.Write(width);writer.Write(height);writer.Write((ushort)1);writer.Write((ushort)24);
                writer.Write(0);writer.Write(bytes);writer.Write(0);writer.Write(0);writer.Write(0);writer.Write(0);
                foreach(Color32 p in pixels.GetPixels32()){writer.Write(p.b);writer.Write(p.g);writer.Write(p.r);}
            }
            Object.Destroy(pixels);_camera.targetTexture=null;_camera.aspect=aspect;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
        }


        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;foreach(var mesh in _owned)if(mesh!=null)Object.Destroy(mesh);
            if(_scene.IsValid()&&_scene.isLoaded){SceneManager.SetActiveScene(SceneManager.CreateScene("Layout cleanup"));yield return SceneManager.UnloadSceneAsync(_scene);}
            TestProfile.End();
        }
    }
}

