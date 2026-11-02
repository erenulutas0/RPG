using UnityEngine;

namespace Cryptforge.UI
{
    // Where the foundry's structures can write depth, so that FoundryVisibilityView can tell which actors no structure can
    // hide in a render. The region's plane (region-local x and y, the meshes' own space) is cut into square cells of
    // CellSize, and each cell keeps the nearest depth (the smallest z) a structure fragment over it can write: +infinity
    // where none can. Every triangle that may write depth (FoundryRegionView.ApplyStructureDepth gives its vertices UV1.x
    // above 0) marks each cell it touches with its nearest vertex's z - the depth across a triangle never comes nearer
    // than its nearest vertex - and a triangle with a moving vertex (a flame tip, a banner's cloth, a gate leaf) marks
    // every cell of the box all its poses stay inside. So a cell never claims less than the structures can write over it,
    // whatever their alpha and whether or not they fade round the hero. Built once with the region, read on every render.
    internal sealed class FoundryDepthCover
    {
        internal const float CellSize = .25f;
        // Room on every boundary for float error, a few millionths of a unit on this map, and far below a cell.
        private const float Slack = 1e-3f;
        private readonly float _left, _bottom, _right, _top;
        private readonly int _columns, _rows;
        private readonly float[] _nearest;

        // Cells over the box from min to max, a cell to spare on every side.
        internal FoundryDepthCover(Vector2 min, Vector2 max)
        {
            _left = min.x - CellSize;
            _bottom = min.y - CellSize;
            _columns = Mathf.Max(1, Mathf.CeilToInt((max.x - _left) / CellSize) + 1);
            _rows = Mathf.Max(1, Mathf.CeilToInt((max.y - _bottom) / CellSize) + 1);
            _right = _left + _columns * CellSize;
            _top = _bottom + _rows * CellSize;
            _nearest = new float[_columns * _rows];
            for (int i = 0; i < _nearest.Length; i++)
                _nearest[i] = float.PositiveInfinity;
        }

        // Every structure has marked its cells. Until then the cover answers that anything may be hidden.
        internal bool Complete { get; set; }
        internal int CellCount => _nearest.Length;

        // A depth-writing triangle where it stands. A triangle over at most two cells each way marks them whole; a larger
        // one marks only the cells it touches, so a long diagonal wall does not claim the open floor beside it.
        internal void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            float near = Mathf.Min(a.z, Mathf.Min(b.z, c.z));
            if (!Cells(Mathf.Min(a.x, Mathf.Min(b.x, c.x)), Mathf.Min(a.y, Mathf.Min(b.y, c.y)),
                    Mathf.Max(a.x, Mathf.Max(b.x, c.x)), Mathf.Max(a.y, Mathf.Max(b.y, c.y)),
                    out int i0, out int j0, out int i1, out int j1))
                return;
            bool small = i1 - i0 <= 1 && j1 - j0 <= 1;
            for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                    if (small || Touches(a, b, c, i, j))
                        Mark(i, j, near);
        }

        // A box that every pose of a moving triangle stays inside, at the triangle's nearest depth (motion moves vertices
        // across the screen, never in depth).
        internal void AddBox(Vector2 min, Vector2 max, float near)
        {
            if (!Cells(min.x, min.y, max.x, max.y, out int i0, out int j0, out int i1, out int j1))
                return;
            for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                    Mark(i, j, near);
        }

        // Whether a structure may write depth over the box (region-local x and y) that is not at least `margin` farther
        // than `far`: any cell under the box nearer than far + margin, a box reaching beyond the cells (none of whose
        // depth is known here) and an unfinished cover all may. Allocates nothing.
        internal bool MayHide(Vector2 min, Vector2 max, float far, float margin)
        {
            float limit = far + margin;
            float x0 = min.x - Slack, y0 = min.y - Slack, x1 = max.x + Slack, y1 = max.y + Slack;
            // Written so that NaN anywhere answers yes.
            if (!Complete || !(x0 >= _left && y0 >= _bottom && x1 < _right && y1 < _top && limit > float.MinValue))
                return true;
            int i0 = Column(x0), j0 = Row(y0), i1 = Column(x1), j1 = Row(y1);
            for (int j = j0; j <= j1; j++)
            {
                int row = j * _columns;
                for (int i = i0; i <= i1; i++)
                    if (_nearest[row + i] < limit)
                        return true;
            }
            return false;
        }

        // The nearest depth a structure can write at a point (region-local), +infinity where none can and outside the
        // cells. For tests.
        internal float NearestAt(Vector2 point)
        {
            if (!(point.x >= _left && point.y >= _bottom && point.x < _right && point.y < _top))
                return float.PositiveInfinity;
            return _nearest[Row(point.y) * _columns + Column(point.x)];
        }

        private int Column(float x) => Mathf.Clamp((int)((x - _left) / CellSize), 0, _columns - 1);
        private int Row(float y) => Mathf.Clamp((int)((y - _bottom) / CellSize), 0, _rows - 1);

        // The cells a box with Slack round it reaches, clipped to the cover; false when it reaches none. What lies beyond
        // the cells needs no mark: MayHide answers yes for any box that reaches there.
        private bool Cells(float x0, float y0, float x1, float y1, out int i0, out int j0, out int i1, out int j1)
        {
            x0 -= Slack; y0 -= Slack; x1 += Slack; y1 += Slack;
            i0 = j0 = i1 = j1 = 0;
            if (!(x1 >= _left && y1 >= _bottom && x0 < _right && y0 < _top))
                return false;
            i0 = Column(Mathf.Max(x0, _left));
            j0 = Row(Mathf.Max(y0, _bottom));
            i1 = Column(Mathf.Min(x1, _right));
            j1 = Row(Mathf.Min(y1, _top));
            return true;
        }

        private void Mark(int i, int j, float near)
        {
            int cell = j * _columns + i;
            if (near < _nearest[cell])
                _nearest[cell] = near;
        }

        // Whether the triangle touches cell (i, j), grown by Slack: the cells come from the triangle's own box, so only
        // its three edges can separate them.
        private bool Touches(Vector3 a, Vector3 b, Vector3 c, int i, int j)
        {
            float cx = _left + (i + .5f) * CellSize, cy = _bottom + (j + .5f) * CellSize, half = CellSize * .5f + Slack;
            return !Separates(a, b, c, cx, cy, half) && !Separates(b, c, a, cx, cy, half) && !Separates(c, a, b, cx, cy, half);
        }

        // Whether the normal of edge p-q is an axis that separates the triangle from the square (centre cx, cy, half size
        // `half`): along it the triangle spans 0 (p and q) to `inside` (its third vertex r), and the square must lie wholly
        // below or wholly above that. A degenerate edge separates nothing.
        private static bool Separates(Vector3 p, Vector3 q, Vector3 r, float cx, float cy, float half)
        {
            float nx = q.y - p.y, ny = p.x - q.x;
            float inside = nx * (r.x - p.x) + ny * (r.y - p.y);
            if (inside < 0f)
            {
                nx = -nx;
                ny = -ny;
                inside = -inside;
            }
            float centre = nx * (cx - p.x) + ny * (cy - p.y);
            float reach = half * (Mathf.Abs(nx) + Mathf.Abs(ny));
            return centre + reach < 0f || centre - reach > inside;
        }
    }
}
