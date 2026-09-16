// Review fixture only. Temporarily copy into Tests/PlayMode, run with graphics, then remove it and its generated meta.
// Loads the real scene under TestProfile isolation; never writes imported game assets or saves the scene.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    public sealed class CharacterProofCapture
    {
        private const string Output = "ArtDirection/2026-09-16/character-unity-01/";
        private readonly List<Object> _owned = new List<Object>();
        private Camera _camera;
        private Scene _scene;
        private Transform _rig;
        private SpriteRenderer _body;
        private readonly List<SpriteRenderer> _mites = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _parts = new List<SpriteRenderer>();
        private readonly List<Vector3> _anchors = new List<Vector3>();
        // Hand centres inspected in the unchanged source, measured from the cropped body's bottom centre.
        private static readonly Vector3 Right = SourcePoint(848, 794);
        private static readonly Vector3 Left = SourcePoint(345, 596);
        private static Vector3 SourcePoint(float x, float y) => new Vector3((x - 594) * 1.375f / 1123, (1213 - y) * 1.375f / 1123, 0);

        [UnityTest]
        public IEnumerator RenderResolutionLoadoutsAndAttachmentMotion()
        {
            Directory.CreateDirectory(Output);
            TestProfile.Begin();
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            _scene = SceneManager.GetActiveScene();
            Time.timeScale = 0;
            _camera = Camera.main;
            var follow = _camera.GetComponent<ArenaCameraFollow>();
            follow.enabled = false;
            foreach (var enemy in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
            foreach (var renderer in follow.Target.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = false;
            // Freeze all behaviours on the original hero; only our static replacement is captured below.
            foreach (var behaviour in follow.Target.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = _camera; canvas.planeDistance = 1;
            }
            foreach (var fitter in Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                fitter.enabled = false;
                var rect = (RectTransform)fitter.transform;
                rect.anchorMin = new Vector2(0, 76f / 2340); rect.anchorMax = new Vector2(1, 1 - 100f / 2340);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            _rig = new GameObject("Candidate rig - review only").transform;
            _owned.Add(_rig.gameObject);
            _body = Renderer(_rig, "Body", 100);
            _body.transform.localPosition = Vector3.zero;
            ContactShadowView.Attach(_rig, _body, .78f, null);
            foreach (var point in new[] {new Vector2(-1.6f,1.1f), new Vector2(1.7f,1.4f), new Vector2(1.7f,1.85f),
                new Vector2(-2.2f,-.8f),new Vector2(2.2f,-1.1f),new Vector2(-.9f,-1.8f),new Vector2(.9f,2.8f),new Vector2(-1,2.6f)})
            {
                var root = new GameObject("Staged Mite"); _owned.Add(root);
                root.transform.position = point;
                var mite = Renderer(root.transform, "Body", 100 - Mathf.RoundToInt(point.y * 10));
                ContactShadowView.Attach(root.transform, mite, .38f, null);
                _mites.Add(mite);
            }
            foreach (HeroWeaponPart part in System.Enum.GetValues(typeof(HeroWeaponPart)))
            {
                var canvas = HeroArt.DrawWeapon(part);
                HeroArt.Pivot(part, out int px, out int py);
                var sprite = PixelSpriteFactory.CreateSprite(canvas, part.ToString(), new Vector2((px+.5f)/canvas.Width,(py+.5f)/canvas.Height));
                _owned.Add(sprite.texture); _owned.Add(sprite);
                var renderer = Renderer(_rig, part.ToString(), 100 + HeroArt.SortingOffset(part));
                renderer.sprite = sprite; _parts.Add(renderer);
                Vector3 anchor = part == HeroWeaponPart.Shield || part == HeroWeaponPart.DaggerLeft ? Left : Right;
                if (part == HeroWeaponPart.StaffOrb) anchor += Vector3.up * (HeroArt.StaffCapRow - HeroArt.StaffGripRow + 4) / 32f;
                _anchors.Add(anchor); renderer.transform.localPosition = anchor;
            }
            yield return null;
            follow.Target.position = Vector3.zero;
            follow.Frame(1080, 2340, 361, 2340-427);
            Object.FindFirstObjectByType<ArenaView>().Backdrop.FrameCavern();
            Canvas.ForceUpdateCanvases();
            SetLoadout(0);
            var metrics = new List<string> {"density,filter,hero_width_texels,hero_height_texels,mite_width_texels,mite_height_texels,hero_height_world,mite_height_world"};
            foreach (int density in new[] {32,64,128})
            {
                foreach (FilterMode filter in density == 128 ? new[] {FilterMode.Point, FilterMode.Bilinear} : new[] {FilterMode.Point})
                {
                    int heroH = density * 44 / 32, heroW = Mathf.RoundToInt(heroH * 612f / 1123);
                    int miteH = density / 2, miteW = Mathf.RoundToInt(miteH * 862f / 841);
                    _body.sprite = Sample("vanguard-body-v2.png", new Rect(288,125,612,1123), heroW, heroH, density, filter);
                    var mite = Sample("cinder-mite-v1.png", new Rect(167,235,862,841), miteW, miteH, density, filter);
                    foreach (var view in _mites) view.sprite = mite;
                    Assert.That(_body.sprite.bounds.size.y, Is.EqualTo(1.375f).Within(.00001f));
                    Assert.That(mite.bounds.size.y, Is.EqualTo(.5f).Within(.00001f));
                    string name = "density-"+density+"-"+filter.ToString().ToLowerInvariant();
                    Capture(name+"-room.png",1080,2340);
                    Detail(name+"-detail.png");
                    metrics.Add($"{density},{filter},{heroW},{heroH},{miteW},{miteH},1.375,0.5");
                }
            }
            File.WriteAllLines(Output+"sampling.csv",metrics);
            // The last candidate is 128 PPU/bilinear. Retain original weapon art so mismatches remain visible.
            for (int loadout=0;loadout<3;loadout++)
            {
                SetLoadout(loadout); Detail("loadout-"+loadout+"-rest.png");
            }
            SetLoadout(0);
            SetLegacyAnchors(); Detail("anchors-legacy.png");
            ResetPose(); Detail("anchors-candidate.png");
            // Transform-only diagnostic: NOT authored walk frames or a completed attack animation.
            float worstGripError = 0;
            for (int frame=0;frame<12;frame++)
            {
                float t=frame/11f;
                _rig.localPosition=new Vector3(Mathf.Sin(t*Mathf.PI*2)*.65f,0,0);
                _parts[0].transform.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(40,-40,t));
                worstGripError=Mathf.Max(worstGripError,Vector3.Distance(_parts[0].transform.position,_rig.TransformPoint(Right)));
                Detail("motion-"+frame.ToString("00")+".png");
            }
            Assert.That(worstGripError,Is.LessThan(.00001f));
            File.WriteAllText(Output+"attachment-check.txt","12 rigid translation/sword-rotation samples; maximum grip transform error: "+worstGripError+" world units. This checks hierarchy attachment, not painted finger occlusion or a walk cycle.\nRight source point: (848,794); left: (345,596); crop bottom centre: (594,1213).\n");
            LogAssert.NoUnexpectedReceived();
        }

        private Sprite Sample(string file, Rect crop, int width, int height, int density, FilterMode filter)
        {
            Texture2D original;
            using(var reader=new BinaryReader(File.OpenRead("TestResults/character-proof-source/"+file+".rgba")))
            {
                int sourceWidth=reader.ReadInt32(),sourceHeight=reader.ReadInt32();
                original=new Texture2D(sourceWidth,sourceHeight,TextureFormat.RGBA32,true);_owned.Add(original);
                original.SetPixelData(reader.ReadBytes(sourceWidth*sourceHeight*4),0);original.Apply(true);
            }
            original.filterMode=FilterMode.Trilinear; original.wrapMode=TextureWrapMode.Clamp;
            var target=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var before=RenderTexture.active;
            // Standard GPU texture sampling; no repainting or changes to the original master.
            Graphics.Blit(original,target,new Vector2(crop.width/original.width,crop.height/original.height),new Vector2(crop.x/original.width,crop.y/original.height));
            RenderTexture.active=target;
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false); _owned.Add(texture);
            texture.ReadPixels(new Rect(0,0,width,height),0,0); texture.Apply(); texture.filterMode=filter; texture.wrapMode=TextureWrapMode.Clamp;
            RenderTexture.active=before; RenderTexture.ReleaseTemporary(target);
            var sprite=Sprite.Create(texture,new Rect(0,0,width,height),new Vector2(.5f,0),density,0,SpriteMeshType.FullRect);
            _owned.Add(sprite); return sprite;
        }

        private static SpriteRenderer Renderer(Transform parent,string name,int order)
        {
            var child=new GameObject(name); child.transform.SetParent(parent,false);
            var renderer=child.AddComponent<SpriteRenderer>(); renderer.sortingOrder=order; return renderer;
        }

        private void ResetPose()
        {
            _rig.localPosition=Vector3.zero;
            for(int i=0;i<_parts.Count;i++){_parts[i].transform.localPosition=_anchors[i];_parts[i].transform.localRotation=Quaternion.identity;}
        }

        private void SetLegacyAnchors()
        {
            for(int i=0;i<_parts.Count;i++)
            {
                HeroArt.Anchor((HeroWeaponPart)i,HeroPose.IdleA,out int x,out int y);
                _parts[i].transform.localPosition=new Vector3((x+.5f-HeroArt.BodyWidth*.5f)/32f,(y+.5f)/32f,0);
            }
        }

        private void SetLoadout(int index)
        {
            ResetPose();
            for(int i=0;i<_parts.Count;i++) _parts[i].enabled=i/2==index;
        }

        private void Detail(string file)
        {
            Vector3 position=_camera.transform.position; float size=_camera.orthographicSize;
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var visible=new List<Canvas>(); foreach(var c in canvases)if(c.enabled){visible.Add(c);c.enabled=false;}
            _camera.transform.position=new Vector3(0,.8f,position.z);_camera.orthographicSize=2.5f;
            Capture(file,600,600);
            _camera.transform.position=position;_camera.orthographicSize=size;
            foreach(var c in visible)c.enabled=true;
        }

        private void Capture(string file,int width,int height)
        {
            var target=RenderTexture.GetTemporary(width,height,24);var before=RenderTexture.active;
            float aspect=_camera.aspect;_camera.aspect=(float)width/height;_camera.targetTexture=target;_camera.Render();RenderTexture.active=target;
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            // ImageConversion is intentionally absent from this project's modules. Export uncompressed RGB evidence.
            using(var writer=new BinaryWriter(File.Create(Output+Path.ChangeExtension(file,"bmp"))))
            {
                int bytes=width*height*3;
                writer.Write((ushort)0x4d42);writer.Write(bytes+54);writer.Write(0);writer.Write(54);
                writer.Write(40);writer.Write(width);writer.Write(height);writer.Write((ushort)1);writer.Write((ushort)24);
                writer.Write(0);writer.Write(bytes);writer.Write(0);writer.Write(0);writer.Write(0);writer.Write(0);
                foreach(Color32 pixel in pixels.GetPixels32()){writer.Write(pixel.b);writer.Write(pixel.g);writer.Write(pixel.r);}
            }
            Object.Destroy(pixels);_camera.targetTexture=null;_camera.aspect=aspect;RenderTexture.active=before;RenderTexture.ReleaseTemporary(target);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale=1;
            if(_scene.IsValid()&&_scene.isLoaded){SceneManager.SetActiveScene(SceneManager.CreateScene("Character capture cleanup"));yield return SceneManager.UnloadSceneAsync(_scene);}
            for(int i=_owned.Count-1;i>=0;i--)if(_owned[i]!=null)Object.Destroy(_owned[i]);
            _owned.Clear(); TestProfile.End();
        }
    }
}
