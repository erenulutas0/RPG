using Cryptforge.Art;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The duel lane's ground (docs/55 section 11): a strip of the Foundry for the two heroes to fight on, built in code
    // like the rest of the arena screen. Back to front: a masonry wall behind the fighters that fades into the page above
    // their heads, a shade at its foot, the Foundry's flagstones as the floor (foreshortened, so it reads as ground and
    // not a wall), a kerb along its front and a shadow under it, and at each end of the lane - the walls of the log, +-6
    // units - a low masonry wall with an iron cap and a brazier burning on it, its light warming the wall and the floor
    // round it.
    //
    // Light on the phone: the textures are the run's own (PlatformMaterialSet's Foundry wall, flagstone and iron, which
    // the scene already holds), so the lane adds no texture memory beyond ArenaUi's small generated sprites, and about
    // twenty images of a handful of textures. Tinted down and warm, so the heroes, the floor marks, the telegraphs and the
    // numbers drawn over it keep their contrast. Everything lies under Root, the lane's first child, behind every figure
    // and effect (DuelPlaybackView.Layer only ever moves those to the front); nothing is a raycast target. Without the
    // materials the same parts stand in plain colours. The braziers flicker on the playback's clock, so a capture's frame
    // is the same on every run.
    internal sealed class DuelLaneBackdrop
    {
        public const string RootName = "Arena Lane Backdrop";
        // Lane coordinates (from the lane's top-left, y down), around DuelPlaybackView.FloorY's feet at 800.
        public const float WallTop = 300f;
        public const float FloorBack = 742f;
        public const float FloorFront = 912f;
        private const float KerbHeight = 22f;
        private const float EndWallWidth = 56f;
        private const float EndWallTop = 612f;
        private const float CapHeight = 18f;
        // Canvas units one repeat of a texture covers: the wall's courses about 52 units, the floor's stones squashed to
        // half their height by the view's angle.
        private const float WallTile = 260f;
        private const float EndWallTile = 200f;
        private const float FloorTile = 240f;

        private static readonly Color WallTint = new Color(.4f, .35f, .37f, 1f);
        private static readonly Color EndWallTint = new Color(.66f, .58f, .56f, 1f);
        private static readonly Color FloorTint = new Color(.5f, .43f, .4f, 1f);
        private static readonly Color KerbTint = new Color(.74f, .6f, .5f, 1f);
        private static readonly Color IronTint = new Color(.8f, .66f, .52f, 1f);
        private static readonly Color BowlTint = new Color(.5f, .42f, .38f, 1f);
        private static readonly Color Brass = new Color(.86f, .62f, .3f, 1f);

        private readonly Image[] _glows = new Image[2];
        private readonly Image[] _flames = new Image[2];
        private readonly Image[] _cores = new Image[2];
        private readonly Image[] _pools = new Image[2];
        private readonly float[] _flameX = new float[2];

        public RectTransform Root { get; }
        public bool UsesFoundryArt { get; }
        public int Braziers => _flames.Length;

        // The lane's own width: the end walls stand just inside its edges, at the log's walls.
        public DuelLaneBackdrop(ArenaUi ui, RectTransform lane, PlatformMaterialSet materials)
        {
            Root = ArenaUi.Node(lane, RootName);
            Root.SetAsFirstSibling();
            ArenaUi.Stretch(Root, Vector2.zero, Vector2.zero);
            Texture wall = TextureOf(materials != null ? materials.FoundryWall : null);
            Texture floor = TextureOf(materials != null ? materials.FoundryFlagstone : null);
            Texture iron = TextureOf(materials != null ? materials.FoundryIron : null);
            UsesFoundryArt = wall != null && floor != null && iron != null;
            float width = lane.rect.width > 0f ? lane.rect.width : 1000f;
            Color page = ArenaUi.Surface;
            page.a = 1f;

            // The wall behind the fighters, fading into the page above their heads, shaded at its foot.
            Tiled(ui, "Arena Lane Wall", wall, WallTint, new Color(.17f, .14f, .16f, 1f), 0f, WallTop, width,
                FloorBack + 10f - WallTop, WallTile, WallTile);
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? EndWallWidth * .5f : width - EndWallWidth * .5f;
                _flameX[side] = x;
                _glows[side] = ui.Part(Root, "Arena Lane Brazier Light " + side, new Color(1f, .52f, .18f, .2f), ui.Disc);
                ArenaUi.Centre(_glows[side].rectTransform, x, EndWallTop - 40f, 520f, 520f);
            }
            Shade(ui, "Arena Lane Wall Fade", page, WallTop, 190f, true);
            Shade(ui, "Arena Lane Wall Foot", new Color(0f, 0f, 0f, .55f), FloorBack - 60f, 70f, false);

            // The floor: the Foundry's flagstones, a contact shade along the wall, a kerb and the drop under it.
            Tiled(ui, "Arena Lane Floor", floor, FloorTint, new Color(.24f, .2f, .19f, 1f), 0f, FloorBack, width,
                FloorFront - FloorBack, FloorTile, FloorTile * .5f);
            Shade(ui, "Arena Lane Floor Contact", new Color(0f, 0f, 0f, .5f), FloorBack, 34f, true);
            RawImage kerb = Tiled(ui, "Arena Lane Kerb", wall, KerbTint, new Color(.36f, .29f, .26f, 1f), 0f, FloorFront,
                width, KerbHeight, WallTile * .7f, WallTile * .7f);
            if (wall != null)
                kerb.uvRect = new Rect(0f, .62f, width / (WallTile * .7f), .2f);
            Shade(ui, "Arena Lane Drop", new Color(0f, 0f, 0f, .7f), FloorFront + KerbHeight, 70f, true);

            // The lane's ends: a low wall at each, its iron cap, and a brazier on the cap.
            for (int side = 0; side < 2; side++)
            {
                string end = side == 0 ? "West" : "East";
                float left = side == 0 ? 0f : width - EndWallWidth;
                float x = _flameX[side];
                _pools[side] = ui.Part(Root, "Arena Lane Brazier Pool " + side, new Color(1f, .5f, .16f, .24f), ui.Disc);
                ArenaUi.Centre(_pools[side].rectTransform, x + (side == 0 ? 60f : -60f), FloorBack + 46f, 360f, 110f);
                Tiled(ui, "Arena Lane " + end + " Wall", wall, EndWallTint, new Color(.3f, .25f, .25f, 1f), left, EndWallTop,
                    EndWallWidth, FloorFront + KerbHeight - EndWallTop, EndWallTile, EndWallTile);
                Image inner = ui.Part(Root, "Arena Lane " + end + " Wall Edge", new Color(0f, 0f, 0f, .4f), ui.White);
                ArenaUi.Place(inner.rectTransform, side == 0 ? EndWallWidth : width - EndWallWidth - 6f, EndWallTop + CapHeight,
                    6f, FloorFront + KerbHeight - EndWallTop - CapHeight);
                RawImage cap = Tiled(ui, "Arena Lane " + end + " Cap", iron, IronTint, new Color(.42f, .36f, .32f, 1f),
                    left - 6f, EndWallTop - CapHeight * .5f, EndWallWidth + 12f, CapHeight, 0f, 0f);
                if (iron != null)
                    cap.uvRect = new Rect(.04f, .9f, .26f, .1f);

                RawImage stand = Tiled(ui, "Arena Lane Brazier Stand " + side, iron, BowlTint, new Color(.2f, .17f, .16f, 1f),
                    x - 9f, EndWallTop - CapHeight * .5f - 18f, 18f, 18f, 0f, 0f);
                if (iron != null)
                    stand.uvRect = new Rect(.4f, .3f, .2f, .3f);
                RawImage bowl = Tiled(ui, "Arena Lane Brazier Bowl " + side, iron, BowlTint, new Color(.2f, .17f, .16f, 1f),
                    x - 30f, EndWallTop - CapHeight * .5f - 44f, 60f, 26f, 0f, 0f);
                if (iron != null)
                    bowl.uvRect = new Rect(.06f, .1f, .88f, .5f);
                Image rim = ui.Part(Root, "Arena Lane Brazier Rim " + side, Brass, ui.White);
                ArenaUi.Place(rim.rectTransform, x - 34f, EndWallTop - CapHeight * .5f - 48f, 68f, 6f);
                _flames[side] = ui.Part(Root, "Arena Lane Brazier Flame " + side, new Color(1f, .5f, .14f, .95f), ui.Disc);
                _cores[side] = ui.Part(Root, "Arena Lane Brazier Core " + side, new Color(1f, .86f, .5f, 1f), ui.Disc);
            }
            Flicker(0f);
        }

        // The braziers at a moment of the playback (its ticks): each flame and its light breathe on their own rhythm.
        public void Flicker(float ticks)
        {
            for (int side = 0; side < 2; side++)
            {
                float a = Mathf.Sin(ticks * .29f + side * 1.7f);
                float b = Mathf.Sin(ticks * .71f + side * .9f);
                float rise = 1f + .09f * a + .05f * b;
                float x = _flameX[side];
                float foot = EndWallTop - CapHeight * .5f - 44f;
                ArenaUi.Centre(_flames[side].rectTransform, x + 2f * b, foot - 34f * rise, 66f, 88f * rise);
                ArenaUi.Centre(_cores[side].rectTransform, x + 1.5f * b, foot - 20f * rise, 34f, 48f * rise);
                Color glow = _glows[side].color;
                _glows[side].color = new Color(glow.r, glow.g, glow.b, .2f + .035f * a);
                Color pool = _pools[side].color;
                _pools[side].color = new Color(pool.r, pool.g, pool.b, .24f + .04f * a);
            }
        }

        private static Texture TextureOf(Material material) => material != null ? material.mainTexture : null;

        // A part of the lane in a texture repeated every tileWidth x tileHeight canvas units (0 for one copy stretched),
        // tinted; in the plain colour where the texture is missing.
        private RawImage Tiled(ArenaUi ui, string name, Texture texture, Color tint, Color plain, float x, float y, float width,
            float height, float tileWidth, float tileHeight)
        {
            RectTransform rect = ArenaUi.Node(Root, name);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.texture = texture != null ? texture : ui.White.texture;
            image.color = texture != null ? tint : plain;
            image.uvRect = texture != null && tileWidth > 0f && tileHeight > 0f
                ? new Rect(0f, 0f, width / tileWidth, height / tileHeight)
                : new Rect(0f, 0f, 1f, 1f);
            ArenaUi.Place(rect, x, y, width, height);
            return image;
        }

        // A band across the lane fading from the colour at its top edge (fromTop) or its bottom edge to clear.
        private void Shade(ArenaUi ui, string name, Color colour, float y, float height, bool fromTop)
        {
            Image shade = ui.Part(Root, name, colour, ui.Fade);
            RectTransform rect = shade.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            // Centred, so the flip for a band fading upward keeps it in place.
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(0f, -(y + height));
            rect.offsetMax = new Vector2(0f, -y);
            if (!fromTop)
                rect.localScale = new Vector3(1f, -1f, 1f);
        }
    }
}
