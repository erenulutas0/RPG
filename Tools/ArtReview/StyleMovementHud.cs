using Cryptforge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.Editor
{
    public static class StyleMovementHud
    {
        public static void Apply()
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            Rect("Hero Bar",64,210,698,22);
            Rect("Stamina Bar",168,175,594,14);
            Rect("Experience Label",62,126,700,32);
            Rect("Experience Bar",64,108,698,8);
            var xp=GameObject.Find("Experience Label").GetComponent<Text>();
            xp.fontSize=25;xp.color=new Color(.68f,.72f,.81f,1);
            var stamina=Object.FindFirstObjectByType<HeroStaminaView>();
            stamina.Fill.color=new Color(.88f,.69f,.38f,1);
            var props=new SerializedObject(stamina);
            props.FindProperty("_readyTrack").colorValue=new Color(.13f,.16f,.23f,1);
            props.FindProperty("_spentTrack").colorValue=new Color(.36f,.17f,.13f,1);
            props.ApplyModifiedPropertiesWithoutUndo();
            var parent=stamina.transform.parent;
            var existing=parent.Find("Movement Label");
            var label=existing==null ? new GameObject("Movement Label",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<Text>() : existing.GetComponent<Text>();
            label.transform.SetParent(parent,false);label.gameObject.layer=stamina.gameObject.layer;
            label.font=xp.font;label.fontSize=22;label.fontStyle=FontStyle.Bold;label.text="MOVE";
            label.color=new Color(.78f,.65f,.46f,1);label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;
            Rect("Movement Label",64,168,94,28);
            EditorUtility.SetDirty(stamina);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        private static void Rect(string name,float x,float y,float width,float height)
        {
            var rect=GameObject.Find(name).GetComponent<RectTransform>();
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;
            rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);
        }
    }
}
