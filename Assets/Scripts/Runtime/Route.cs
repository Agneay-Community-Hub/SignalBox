using System.Collections.Generic;
using UnityEngine;

namespace SignalBox
{
    /// A polyline a train follows, sampled by distance.
    public class Route
    {
        readonly List<Vector3> pts;
        readonly List<float> cum;

        public float Length => cum[cum.Count - 1];

        public Route(List<Vector3> points)
        {
            pts = points;
            cum = new List<float> { 0f };
            for (int i = 1; i < pts.Count; i++) cum.Add(cum[i - 1] + Vector3.Distance(pts[i - 1], pts[i]));
        }

        public Vector3 Pos(float d)
        {
            if (d <= 0f) return pts[0] + (pts[1] - pts[0]).normalized * d;
            if (d >= Length)
            {
                int n = pts.Count;
                return pts[n - 1] + (pts[n - 1] - pts[n - 2]).normalized * (d - Length);
            }
            int i = cum.BinarySearch(d);
            if (i < 0) i = ~i;
            int a = Mathf.Max(1, i);
            float t = (d - cum[a - 1]) / Mathf.Max(1e-4f, cum[a] - cum[a - 1]);
            return Vector3.Lerp(pts[a - 1], pts[a], t);
        }

        public Vector3 Dir(float d)
        {
            var v = Pos(d + 1f) - Pos(d - 1f);
            return v.sqrMagnitude < 1e-6f ? Vector3.right : v.normalized;
        }

        public static List<Vector3> CatmullRom(IList<Vector3> c, float step)
        {
            var o = new List<Vector3>();
            for (int i = 0; i < c.Count - 1; i++)
            {
                Vector3 p0 = c[Mathf.Max(i - 1, 0)], p1 = c[i], p2 = c[i + 1], p3 = c[Mathf.Min(i + 2, c.Count - 1)];
                int n = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(p1, p2) / step));
                for (int j = 0; j < n; j++)
                {
                    float t = j / (float)n, t2 = t * t, t3 = t2 * t;
                    o.Add(0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            o.Add(c[c.Count - 1]);
            return o;
        }
    }
}
