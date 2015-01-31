using Delaunay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Map
{
    public class Graph
    {
        List<KeyValuePair<int, Corner>> _cornerMap = new List<KeyValuePair<int, Corner>>();
        Func<Vector2, bool> inside;
        bool _needsMoreRandomness;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public List<Center> centers = new List<Center>();
        public List<Corner> corners = new List<Corner>();
        public List<Edge> edges = new List<Edge>();

        private List<Corner> LandCorners { get { return corners.Where(p => !p.ocean && !p.coast).ToList(); } }

        public Graph(IEnumerable<Vector2> points, Voronoi voronoi, int width, int height, float lakeThreshold)
        {
            Width = width;
            Height = height;
            inside = IslandShape.makePerlin();

            BuildGraph(points, voronoi);
            AssignCornerElevations();
            AssignOceanCoastAndLand(lakeThreshold);
            RedistributeElevations();

            AssignPolygonElevations();

            // Determine downslope paths.
            CalculateDownslopes();

            // Determine watersheds: for every corner, where does it flow
            // out into the ocean? 
            CalculateWatersheds();

            // Create rivers.
            CreateRivers();

            // Determine moisture at corners, starting at rivers
            // and lakes, but not oceans. Then redistribute
            // moisture to cover the entire range evenly from 0.0
            // to 1.0. Then assign polygon moisture as the average
            // of the corner moisture.
            AssignCornerMoisture();
            RedistributeMoisture();
            AssignPolygonMoisture();

            centers.ForEach(p => p.biome = GetBiome(p));
        }

        private void BuildGraph(IEnumerable<Vector2> points, Delaunay.Voronoi voronoi)
        {
            // Build graph data structure in 'edges', 'centers', 'corners',
            // based on information in the Voronoi results: point.neighbors
            // will be a list of neighboring points of the same type (corner
            // or center); point.edges will be a list of edges that include
            // that point. Each edge connects to four points: the Voronoi edge
            // edge.{v0,v1} and its dual Delaunay triangle edge edge.{d0,d1}.
            // For boundary polygons, the Delaunay edge will have one null
            // point, and the Voronoi edge may be null.
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
                    v0 = MakeCorner(vedge.p0),
                    v1 = MakeCorner(vedge.p1),
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
                    AddToCenterList(edge.d0.neighbors, edge.d1);
                    AddToCenterList(edge.d1.neighbors, edge.d0);
                }

                // Corners point to corners
                if (edge.v0 != null && edge.v1 != null)
                {
                    AddToCornerList(edge.v0.adjacent, edge.v1);
                    AddToCornerList(edge.v1.adjacent, edge.v0);
                }

                // Centers point to corners
                if (edge.d0 != null)
                {
                    AddToCornerList(edge.d0.corners, edge.v0);
                    AddToCornerList(edge.d0.corners, edge.v1);
                }
                if (edge.d1 != null)
                {
                    AddToCornerList(edge.d1.corners, edge.v0);
                    AddToCornerList(edge.d1.corners, edge.v1);
                }

                // Corners point to centers
                if (edge.v0 != null)
                {
                    AddToCenterList(edge.v0.touches, edge.d0);
                    AddToCenterList(edge.v0.touches, edge.d1);
                }
                if (edge.v1 != null)
                {
                    AddToCenterList(edge.v1.touches, edge.d0);
                    AddToCenterList(edge.v1.touches, edge.d1);
                }
            }

            // TODO: use edges to determine these
            var topLeft = centers.OrderBy(p => p.point.x + p.point.y).First();
            AddCorner(topLeft, 0, 0);

            var bottomRight = centers.OrderByDescending(p => p.point.x + p.point.y).First();
            AddCorner(bottomRight, Width, Height);

            var topRight = centers.OrderByDescending(p => Width - p.point.x + p.point.y).First();
            AddCorner(topRight, 0, Height);

            var bottomLeft = centers.OrderByDescending(p => p.point.x + Height - p.point.y).First();
            AddCorner(bottomLeft, Width, 0);

            // required for polygon fill
            foreach (var center in centers)
            {
                center.corners.Sort(ClockwiseComparison(center));
            }
        }

        private static void AddCorner(Center topLeft, int x, int y)
        {
            if (topLeft.point.x != x || topLeft.point.y != y)
                topLeft.corners.Add(new Corner { ocean = true, point = new Vector2(x, y) });
        }

        private Comparison<Corner> ClockwiseComparison(Center center)
        {
            Comparison<Corner> result =
                (a, b) =>
                {
                    return (int)(((a.point.x - center.point.x) * (b.point.y - center.point.y) - (b.point.x - center.point.x) * (a.point.y - center.point.y)) * 1000);
                };
            return result;
        }

        private Corner MakeCorner(Vector2? nullablePoint)
        {
            // The Voronoi library generates multiple Point objects for
            // corners, and we need to canonicalize to one Corner object.
            // To make lookup fast, we keep an array of Points, bucketed by
            // x value, and then we only have to look at other Points in
            // nearby buckets. When we fail to find one, we'll create a new
            // Corner object.

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

        private void AddToCornerList(List<Corner> v, Corner x)
        {
            if (x != null && v.IndexOf(x) < 0)
                v.Add(x);
        }

        private void AddToCenterList(List<Center> v, Center x)
        {
            if (x != null && v.IndexOf(x) < 0) { v.Add(x); }
        }

        private void AssignCornerElevations()
        {
            // Determine elevations and water at Voronoi corners. By
            // construction, we have no local minima. This is important for
            // the downslope vectors later, which are used in the river
            // construction algorithm. Also by construction, inlets/bays
            // push low elevation areas inland, which means many rivers end
            // up flowing out through them. Also by construction, lakes
            // often end up on river paths because they don't raise the
            // elevation as much as other terrain does.

            //var q:Corner, s:Corner;
            var queue = new Queue<Corner>();

            foreach (var q in corners)
            {
                q.water = !inside(q.point);
            }

            foreach (var q in corners)
            {
                // The edges of the map are elevation 0
                if (q.border)
                {
                    q.elevation = 0;
                    queue.Enqueue(q);
                }
                else
                {
                    q.elevation = float.PositiveInfinity;
                }
            }

            // Traverse the graph and assign elevations to each point. As we
            // move away from the map border, increase the elevations. This
            // guarantees that rivers always have a way down to the coast by
            // going downhill (no local minima).
            while (queue.Any())
            {
                var q = queue.Dequeue();

                foreach (var s in q.adjacent)
                {
                    // Every step up is epsilon over water or 1 over land. The
                    // number doesn't matter because we'll rescale the
                    // elevations later.
                    var newElevation = 0.01f + q.elevation;
                    if (!q.water && !s.water)
                    {
                        newElevation += 1;
                        if (_needsMoreRandomness)
                        {
                            // HACK: the map looks nice because of randomness of
                            // points, randomness of rivers, and randomness of
                            // edges. Without random point selection, I needed to
                            // inject some more randomness to make maps look
                            // nicer. I'm doing it here, with elevations, but I
                            // think there must be a better way. This hack is only
                            // used with square/hexagon grids.
                            newElevation += UnityEngine.Random.value; ;
                        }
                    }
                    // If this point changed, we'll add it to the queue so
                    // that we can process its neighbors too.
                    if (newElevation < s.elevation)
                    {
                        s.elevation = newElevation;
                        queue.Enqueue(s);
                    }
                }
            }
        }

        private void AssignOceanCoastAndLand(float lakeThreshold)
        {
            // Compute polygon attributes 'ocean' and 'water' based on the
            // corner attributes. Count the water corners per
            // polygon. Oceans are all polygons connected to the edge of the
            // map. In the first pass, mark the edges of the map as ocean;
            // in the second pass, mark any water-containing polygon
            // connected an ocean as ocean.
            var queue = new Queue<Center>();
            //var p:Center, q:Corner, r:Center, numWater:int;

            foreach (var p in centers)
            {
                var numWater = 0;
                foreach (var q in p.corners)
                {
                    if (q.border)
                    {
                        p.border = true;
                        p.ocean = true;
                        q.water = true;
                        queue.Enqueue(p);
                    }

                    if (q.water)
                        numWater += 1;

                }
                p.water = (p.ocean || numWater >= p.corners.Count * lakeThreshold);
            }
            while (queue.Any())
            {
                var p = queue.Dequeue();
                foreach (var r in p.neighbors)
                {
                    if (r.water && !r.ocean)
                    {
                        r.ocean = true;
                        queue.Enqueue(r);
                    }
                }
            }

            // Set the polygon attribute 'coast' based on its neighbors. If
            // it has at least one ocean and at least one land neighbor,
            // then this is a coastal polygon.
            foreach (var p in centers)
            {
                var numOcean = 0;
                var numLand = 0;
                foreach (var r in p.neighbors)
                {
                    numOcean += r.ocean ? 1 : 0;
                    numLand += !r.water ? 1 : 0;
                }
                p.coast = (numOcean > 0) && (numLand > 0);
            }

            // Set the corner attributes based on the computed polygon
            // attributes. If all polygons connected to this corner are
            // ocean, then it's ocean; if all are land, then it's land;
            // otherwise it's coast.
            foreach (var q in corners)
            {
                var numOcean = 0;
                var numLand = 0;
                foreach (var p in q.touches)
                {
                    numOcean += p.ocean ? 1 : 0;
                    numLand += !p.water ? 1 : 0;
                }
                q.ocean = (numOcean == q.touches.Count);
                q.coast = (numOcean > 0) && (numLand > 0);
                q.water = q.border || ((numLand != q.touches.Count) && !q.coast);
            }
        }

        private void RedistributeElevations()
        {
            // Change the overall distribution of elevations so that lower
            // elevations are more common than higher
            // elevations. Specifically, we want elevation X to have frequency
            // (1-X).  To do this we will sort the corners, then set each
            // corner to its desired elevation.

            var locations = LandCorners;
            // SCALE_FACTOR increases the mountain area. At 1.0 the maximum
            // elevation barely shows up on the map, so we set it to 1.1.
            var SCALE_FACTOR = 1.1f;
            locations.Sort((a, b) => a.elevation.CompareTo(b.elevation));
            for (int i = 0; i < locations.Count; i++)
            {
                // Let y(x) be the total area that we want at elevation <= x.
                // We want the higher elevations to occur less than lower
                // ones, and set the area to be y(x) = 1 - (1-x)^2.
                var y = (float)i / (locations.Count - 1);
                // Now we have to solve for x, given the known y.
                //  *  y = 1 - (1-x)^2
                //  *  y = 1 - (1 - 2x + x^2)
                //  *  y = 2x - x^2
                //  *  x^2 - 2x + y = 0
                // From this we can use the quadratic equation to get:
                float x = Mathf.Sqrt(SCALE_FACTOR) - Mathf.Sqrt(SCALE_FACTOR * (1 - y));
                if (x > 1.0) x = 1.0f;  // TODO: does this break downslopes?
                locations[i].elevation = x;
            }

            // Assign elevations to non-land corners
            corners.Where(p => p.ocean || p.coast).ToList().ForEach(p => p.elevation = 0);
        }

        private void AssignPolygonElevations()
        {
            // Polygon elevations are the average of their corners
            foreach (var p in centers)
            {
                var sumElevation = 0.0f;
                foreach (var q in p.corners)
                {
                    sumElevation += q.elevation;
                }
                p.elevation = sumElevation / p.corners.Count;
            }
        }

        private void CalculateDownslopes()
        {
            // Calculate downslope pointers.  At every point, we point to the
            // point downstream from it, or to itself.  This is used for
            // generating rivers and watersheds.

            foreach (var q in corners)
            {
                var r = q;
                foreach (var s in q.adjacent)
                {
                    if (s.elevation <= r.elevation)
                    {
                        r = s;
                    }
                }
                q.downslope = r;
            }
        }

        private void CalculateWatersheds()
        {
            // Calculate the watershed of every land point. The watershed is
            // the last downstream land point in the downslope graph. TODO:
            // watersheds are currently calculated on corners, but it'd be
            // more useful to compute them on polygon centers so that every
            // polygon can be marked as being in one watershed.

            // Initially the watershed pointer points downslope one step.      
            foreach (var q in corners)
            {
                q.watershed = q;
                if (!q.ocean && !q.coast)
                {
                    q.watershed = q.downslope;
                }
            }
            // Follow the downslope pointers to the coast. Limit to 100
            // iterations although most of the time with numPoints==2000 it
            // only takes 20 iterations because most points are not far from
            // a coast.  TODO: can run faster by looking at
            // p.watershed.watershed instead of p.downslope.watershed.
            for (var i = 0; i < 100; i++)
            {
                var changed = false;
                foreach (var q in corners)
                {
                    if (!q.ocean && !q.coast && !q.watershed.coast)
                    {
                        var r = q.downslope.watershed;
                        if (!r.ocean) q.watershed = r;
                        changed = true;
                    }
                }
                if (!changed) break;
            }
            // How big is each watershed?
            foreach (var q in corners)
            {
                var r = q.watershed;
                r.watershed_size = 1 + r.watershed_size;
            }
        }

        private void CreateRivers()
        {
            // Create rivers along edges. Pick a random corner point, then
            // move downslope. Mark the edges and corners as rivers.
            for (var i = 0; i < (Width + Height) / 4; i++)
            {
                var q = corners[UnityEngine.Random.Range(0, corners.Count - 1)];
                if (q.ocean || q.elevation < 0.3 || q.elevation > 0.9) continue;
                // Bias rivers to go west: if (q.downslope.x > q.x) continue;
                while (!q.coast)
                {
                    if (q == q.downslope)
                    {
                        break;
                    }
                    var edge = lookupEdgeFromCorner(q, q.downslope);
                    edge.river = edge.river + 1;
                    q.river++;
                    q.downslope.river++;  // TODO: fix double count
                    q = q.downslope;
                }
            }
        }

        private void AssignCornerMoisture()
        {
            // Calculate moisture. Freshwater sources spread moisture: rivers
            // and lakes (not oceans). Saltwater sources have moisture but do
            // not spread it (we set it at the end, after propagation).

            var queue = new Queue<Corner>();
            // Fresh water
            foreach (var q in corners)
            {
                if ((q.water || q.river > 0) && !q.ocean)
                {
                    q.moisture = q.river > 0 ? Mathf.Min(3.0f, (0.2f * q.river)) : 1.0f;
                    queue.Enqueue(q);
                }
                else
                {
                    q.moisture = 0;
                }
            }
            while (queue.Any())
            {
                var q = queue.Dequeue();

                foreach (var r in q.adjacent)
                {
                    var newMoisture = q.moisture * 0.9f;
                    if (newMoisture > r.moisture)
                    {
                        r.moisture = newMoisture;
                        queue.Enqueue(r);
                    }
                }
            }
            // Salt water
            foreach (var q in corners)
            {
                if (q.ocean || q.coast)
                {
                    q.moisture = 1.0f;
                }
            }
        }

        private void AssignPolygonMoisture()
        {
            // Polygon moisture is the average of the moisture at corners
            foreach (var p in centers)
            {
                var sumMoisture = 0.0f;
                foreach (var q in p.corners)
                {
                    if (q.moisture > 1.0)
                        q.moisture = 1.0f;
                    sumMoisture += q.moisture;
                }
                p.moisture = sumMoisture / p.corners.Count;
            }
        }

        private Edge lookupEdgeFromCenter(Center p, Center r)
        {
            foreach (var edge in p.borders)
            {
                if (edge.d0 == r || edge.d1 == r)
                    return edge;
            }
            return null;
        }

        private Edge lookupEdgeFromCorner(Corner q, Corner s)
        {
            foreach (var edge in q.protrudes)
            {
                if (edge.v0 == s || edge.v1 == s)
                    return edge;
            }
            return null;
        }

        private void RedistributeMoisture()
        {
            // Change the overall distribution of moisture to be evenly distributed.
            var locations = LandCorners;
            locations.Sort((a, b) => a.moisture.CompareTo(b.moisture));

            for (var i = 0; i < locations.Count; i++)
            {
                locations[i].moisture = (float)i / (locations.Count - 1);
            }
        }

        static Biome GetBiome(Center p)
        {
            if (p.ocean)
            {
                return Biome.Ocean;
            }
            else if (p.water)
            {
                if (p.elevation < 0.1) return Biome.Marsh;
                if (p.elevation > 0.8) return Biome.Ice;
                return Biome.Lake;
            }
            else if (p.coast)
            {
                return Biome.Beach;
            }
            else if (p.elevation > 0.8)
            {
                if (p.moisture > 0.50) return Biome.Snow;
                else if (p.moisture > 0.33) return Biome.Tundra;
                else if (p.moisture > 0.16) return Biome.Bare;
                else return Biome.Scorched;
            }
            else if (p.elevation > 0.6)
            {
                if (p.moisture > 0.66) return Biome.Taiga;
                else if (p.moisture > 0.33) return Biome.Shrubland;
                else return Biome.TemperateDesert;
            }
            else if (p.elevation > 0.3)
            {
                if (p.moisture > 0.83) return Biome.TemperateRainForest;
                else if (p.moisture > 0.50) return Biome.TemperateDeciduousForest;
                else if (p.moisture > 0.16) return Biome.Grassland;
                else return Biome.TemperateDesert;
            }
            else
            {
                if (p.moisture > 0.66) return Biome.TropicalRainForest;
                else if (p.moisture > 0.33) return Biome.TropicalSeasonalForest;
                else if (p.moisture > 0.16) return Biome.Grassland;
                else return Biome.SubtropicalDesert;
            }
        }

        public static IEnumerable<Vector2> RelaxPoints(IEnumerable<Vector2> startingPoints, float width, float height)
        {
            Delaunay.Voronoi v = new Delaunay.Voronoi(startingPoints.ToList(), null, new Rect(0, 0, width, height));
            foreach (var point in startingPoints)
            {
                var region = v.Region(point);
                point.Set(0, 0);
                foreach (var r in region)
                    point.Set(point.x + r.x, point.y + r.y);

                point.Set(point.x / region.Count, point.y / region.Count);
                yield return point;
            }
        }
    }
}
