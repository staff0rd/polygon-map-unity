using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Graph
{
    public class Graph
    {
        List<KeyValuePair<int, Corner>> _cornerMap = new List<KeyValuePair<int, Corner>>();

        public int Width { get; private set; }
        public int Height { get; private set; }
        public List<Center> centers = new List<Center>();
        public List<Corner> corners = new List<Corner>();
        public List<Edge> edges = new List<Edge>();

        public Graph(IEnumerable<Vector2> points, Delaunay.Voronoi voronoi, int width, int height)
        {
            Width = width;
            Height = height;

            //var p:Center, q:Corner, point:Point, other:Point;
            var libedges = voronoi.Edges();

            var centerLookup = new Dictionary<Vector2?, Center>();

            // Build Center objects for each of the points, and a lookup map
            // to find those Center objects again as we build the graph
            foreach (var point in points)
            {
                var p = new Center { index = centers.Count, point = point };
                centers.Add(p);
                centerLookup[point] = p;
            }

            // Workaround for Voronoi lib bug: we need to call region()
            // before Edges or neighboringSites are available
            foreach (var p in centers)
            {
                voronoi.Region(p.point);
            }

            foreach (var libedge in libedges)
            {
                var dedge = libedge.DelaunayLine();
                var vedge = libedge.VoronoiEdge();

                // Fill the graph data. Make an Edge object corresponding to
                // the edge from the voronoi library.
                var edge = new Edge
                {
                    index = edges.Count,
                    river = 0,

                    // Edges point to corners. Edges point to centers. 
                    v0 = makeCorner(vedge.p0),
                    v1 = makeCorner(vedge.p1),
                    d0 = centerLookup[dedge.p0],
                    d1 = centerLookup[dedge.p1]
                };
                if (vedge.p0.HasValue && vedge.p1.HasValue)
                    edge.midpoint = Vector2Extensions.Interpolate(vedge.p0.Value, vedge.p1.Value, 0.5f);

                edges.Add(edge);

                // Centers point to edges. Corners point to edges.
                if (edge.d0 != null) { edge.d0.borders.Add(edge); }
                if (edge.d1 != null) { edge.d1.borders.Add(edge); }
                if (edge.v0 != null) { edge.v0.protrudes.Add(edge); }
                if (edge.v1 != null) { edge.v1.protrudes.Add(edge); }

                // Centers point to centers.
                if (edge.d0 != null && edge.d1 != null)
                {
                    addToCenterList(edge.d0.neighbors, edge.d1);
                    addToCenterList(edge.d1.neighbors, edge.d0);
                }

                // Corners point to corners
                if (edge.v0 != null && edge.v1 != null)
                {
                    addToCornerList(edge.v0.adjacent, edge.v1);
                    addToCornerList(edge.v1.adjacent, edge.v0);
                }

                // Centers point to corners
                if (edge.d0 != null)
                {
                    addToCornerList(edge.d0.corners, edge.v0);
                    addToCornerList(edge.d0.corners, edge.v1);
                }
                if (edge.d1 != null)
                {
                    addToCornerList(edge.d1.corners, edge.v0);
                    addToCornerList(edge.d1.corners, edge.v1);
                }

                // Corners point to centers
                if (edge.v0 != null)
                {
                    addToCenterList(edge.v0.touches, edge.d0);
                    addToCenterList(edge.v0.touches, edge.d1);
                }
                if (edge.v1 != null)
                {
                    addToCenterList(edge.v1.touches, edge.d0);
                    addToCenterList(edge.v1.touches, edge.d1);
                }
            }
        }

        // The Voronoi library generates multiple Point objects for
        // corners, and we need to canonicalize to one Corner object.
        // To make lookup fast, we keep an array of Points, bucketed by
        // x value, and then we only have to look at other Points in
        // nearby buckets. When we fail to find one, we'll create a new
        // Corner object.
        private Corner makeCorner(Vector2? nullablePoint)
        {
            if (nullablePoint == null)
                return null;

            var point = nullablePoint.Value;

            for (var i = (int)(point.x - 1); i <= (int)(point.x + 1); i++)
            {
                foreach (var kvp in _cornerMap.Where(p => p.Key == i))
                {
                    var dx = point.x - kvp.Value.point.x;
                    var dy = point.y - kvp.Value.point.y;
                    if (dx * dx + dy * dy < 1e-6)
                        return kvp.Value;
                }
            }

            var corner = new Corner { index = corners.Count, point = point };
            corners.Add(corner);
            corner.border = point.x == 0 || point.x == Width || point.y == 0 || point.y == Height;

            _cornerMap.Add(new KeyValuePair<int, Corner>((int)(point.x), corner));

            return corner;
        }

        // Helper functions for the following for loop; ideally these
        // would be inlined
        private void addToCornerList(List<Corner> v, Corner x)
        {
            if (x != null && v.IndexOf(x) < 0)
                v.Add(x);
        }
        private void addToCenterList(List<Center> v, Center x)
        {
            if (x != null && v.IndexOf(x) < 0) { v.Add(x); }
        }
    }
}
