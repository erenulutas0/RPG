using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.Art
{
    // Generated once from the fixed VoidLayout. Only GPU sprites and small attachment metadata survive generation;
    // the large CPU canvases, including island canvases used to locate chain anchors, can be collected immediately.
    internal sealed class VoidSprites : IDisposable
    {
        private static readonly Vector2 TopCentre = new Vector2(.5f, 1f);
        private readonly List<Sprite> _owned = new List<Sprite>();
        private readonly Sprite[] _hazes;
        private readonly Sprite[] _stars = new Sprite[2];
        private readonly IslandSprites[] _islands;
        private readonly Sprite[] _rubble;
        internal VoidScene Scene { get; }
        internal Sprite Galaxy { get; }
        internal Sprite SparkleLit { get; }
        internal Sprite SparkleDim { get; }
        internal Sprite FlameTall { get; }
        internal Sprite FlameLean { get; }

        internal VoidSprites()
        {
            Scene = VoidLayout.Create();
            _hazes = new Sprite[Scene.Hazes.Length];
            _islands = new IslandSprites[Scene.Islands.Length];
            _rubble = new Sprite[Scene.Rubble.Length];
            try
            {
                for (int i = 0; i < _hazes.Length; i++)
                {
                    HazePlacement haze = Scene.Hazes[i];
                    _hazes[i] = Make(VoidArt.DrawHaze(haze.Width, haze.Height, haze.Seed), "Void Haze " + i,
                        PixelSpriteFactory.Centre, VoidArt.HazeTexelsPerUnit);
                }
                GalaxyPlacement galaxy = Scene.Galaxy;
                Galaxy = Make(VoidArt.DrawGalaxy(galaxy.Radius, galaxy.Seed), "Void Galaxy", PixelSpriteFactory.Centre);
                int width = Mathf.RoundToInt(VoidScene.StarFieldWidth * VoidArt.TexelsPerUnit);
                int height = Mathf.RoundToInt(VoidScene.StarFieldHeight * VoidArt.TexelsPerUnit);
                for (int i = 0; i < _stars.Length; i++)
                    _stars[i] = Make(VoidArt.DrawStarField(width, height, VoidScene.StarFieldSeed, i), "Void Stars " + i, TopCentre);
                SparkleLit = Make(VoidArt.DrawSparkle(true), "Void Sparkle Lit", PixelSpriteFactory.Centre);
                SparkleDim = Make(VoidArt.DrawSparkle(false), "Void Sparkle Dim", PixelSpriteFactory.Centre);
                FlameTall = Make(VoidArt.DrawLantern(0), "Void Flame Tall", PixelSpriteFactory.BottomCentre);
                FlameLean = Make(VoidArt.DrawLantern(1), "Void Flame Lean", PixelSpriteFactory.BottomCentre);
                for (int i = 0; i < _islands.Length; i++)
                {
                    IslandPlacement placement = Scene.Islands[i];
                    IslandArt art = VoidArt.DrawIsland(placement.Spec);
                    Sprite back = null;
                    Sprite front = null;
                    if (placement.HasRing)
                    {
                        back = Make(VoidArt.DrawRing(placement.RingWidth, placement.RingHeight, false), "Void Ring Back " + i, PixelSpriteFactory.Centre);
                        front = Make(VoidArt.DrawRing(placement.RingWidth, placement.RingHeight, true), "Void Ring Front " + i, PixelSpriteFactory.Centre);
                    }
                    Sprite rock = Make(art.Canvas, "Void Island " + i, PixelSpriteFactory.BottomCentre);
                    var chains = new Sprite[placement.Chains.Length];
                    var chainAnchors = new Vector3[chains.Length];
                    for (int c = 0; c < chains.Length; c++)
                    {
                        ChainSpec chain = placement.Chains[c];
                        chains[c] = Make(VoidArt.DrawChain(chain.Links), "Void Chain " + i + "." + c, TopCentre);
                        placement.ChainTop(art, chain, out float x, out float y);
                        chainAnchors[c] = new Vector3(x - placement.X, y - placement.Y, 0f);
                    }
                    var ringAnchor = new Vector3(0f, placement.RingCentreY(art) - placement.Y, 0f);
                    var flameAnchor = new Vector3((art.LanternX + .5f - placement.Spec.Width / 2f) / VoidArt.TexelsPerUnit,
                        art.LanternY / (float)VoidArt.TexelsPerUnit, 0f);
                    _islands[i] = new IslandSprites(rock, back, front, ringAnchor, flameAnchor, chains, chainAnchors);
                }
                for (int i = 0; i < _rubble.Length; i++)
                {
                    RubblePlacement rubble = Scene.Rubble[i];
                    _rubble[i] = Make(VoidArt.DrawRubble(rubble.Size, rubble.Seed), "Void Rubble " + i, PixelSpriteFactory.Centre);
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal Sprite Haze(int index) => _hazes[index];
        internal Sprite Stars(int layer) => _stars[layer];
        internal IslandSprites Island(int index) => _islands[index];
        internal Sprite Rubble(int index) => _rubble[index];
        internal int SpriteCount => _owned.Count;
        internal Sprite SpriteAt(int index) => _owned[index];

        public void Dispose()
        {
            foreach (Sprite sprite in _owned)
                PixelSpriteFactory.Destroy(sprite);
            _owned.Clear();
        }

        private Sprite Make(PixelCanvas canvas, string name, Vector2 pivot, float pixelsPerUnit = PixelSpriteFactory.PixelsPerUnit)
        {
            Sprite sprite = PixelSpriteFactory.CreateSprite(canvas, name, pivot, pixelsPerUnit);
            _owned.Add(sprite);
            return sprite;
        }
    }

    internal sealed class IslandSprites
    {
        private readonly Sprite[] _chains;
        private readonly Vector3[] _chainAnchors;
        internal Sprite Rock { get; }
        internal Sprite RingBack { get; }
        internal Sprite RingFront { get; }
        internal Vector3 RingAnchor { get; }
        internal Vector3 FlameAnchor { get; }

        internal IslandSprites(Sprite rock, Sprite ringBack, Sprite ringFront, Vector3 ringAnchor, Vector3 flameAnchor,
            Sprite[] chains, Vector3[] chainAnchors)
        {
            Rock = rock;
            RingBack = ringBack;
            RingFront = ringFront;
            RingAnchor = ringAnchor;
            FlameAnchor = flameAnchor;
            _chains = chains;
            _chainAnchors = chainAnchors;
        }

        internal Sprite Chain(int index) => _chains[index];
        internal Vector3 ChainAnchor(int index) => _chainAnchors[index];
    }
}
