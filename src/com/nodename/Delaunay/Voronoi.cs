/*
 * The author of this software is Steven Fortune.  Copyright (c) 1994 by AT&T
 * Bell Laboratories.
 * Permission to use, copy, modify, and distribute this software for any
 * purpose without fee is hereby granted, provided that this entire notice
 * is included in all copies of any software which is or includes a copy
 * or modification of this software and in all copies of the supporting
 * documentation for such software.
 * THIS SOFTWARE IS BEING PROVIDED "AS IS", WITHOUT ANY EXPRESS OR IMPLIED
 * WARRANTY.  IN PARTICULAR, NEITHER THE AUTHORS NOR AT&T MAKE ANY
 * REPRESENTATION OR WARRANTY OF ANY KIND CONCERNING THE MERCHANTABILITY
 * OF THIS SOFTWARE OR ITS FITNESS FOR ANY PARTICULAR PURPOSE.
 */

using UnityEngine;
using System;
using System.Collections.Generic;
using Delaunay.Geo;
using Delaunay.Utils;
using Delaunay.LR;

namespace Delaunay
{
	public sealed class Voronoi: Utils.IDisposable
	{
		private SiteList _sites;
		private Dictionary <Vector2,Site> _sitesIndexedByLocation;
		private List<Triangle> _triangles;
		private List<Edge> _edges;

		
		// TODO generalize this so it doesn't have to be a rectangle;
		// then we can make the fractal voronois-within-voronois
		private Rect _plotBounds;
		public Rect plotBounds {
			get { return _plotBounds;}
		}
		
		public void Dispose ()
		{
			int i, n;
			if (_sites != null) {
				_sites.Dispose ();
				_sites = null;
			}
			if (_triangles != null) {
				n = _triangles.Count;
				for (i = 0; i < n; ++i) {
					_triangles [i].Dispose ();
				}
				_triangles.Clear ();
				_triangles = null;
			}
			if (_edges != null) {
				n = _edges.Count;
				for (i = 0; i < n; ++i) {
					_edges [i].Dispose ();
				}
				_edges.Clear ();
				_edges = null;
			}
//			_plotBounds = null;
			_sitesIndexedByLocation = null;
		}
		
		public Voronoi (List<Vector2> points, List<uint> colors, Rect plotBounds)
		{
			_sites = new SiteList ();
			_sitesIndexedByLocation = new Dictionary <Vector2,Site> (); // XXX: Used to be Dictionary(true) -- weak refs. 
			addSites (points, colors);
			_plotBounds = plotBounds;
			_triangles = new List<Triangle> ();
			_edges = new List<Edge> ();
			fortunesAlgorithm ();
		}
		
		private void addSites (List<Vector2> points, List<uint> colors)
		{
			int length = points.Count;
			for (int i = 0; i < length; ++i) {
				addSite (points [i], (colors != null) ? colors [i] : 0, i);
			}
		}
		
		private void addSite (Vector2 p, uint color, int index)
		{
			if (_sitesIndexedByLocation.ContainsKey (p))
				return; // Prevent duplicate site! (Adapted from https://github.com/nodename/as3delaunay/issues/1)
			float weight = UnityEngine.Random.value * 100f;
			Site site = Site.Create (p, (uint)index, weight, color);
			_sites.Add (site);
			_sitesIndexedByLocation [p] = site;
		}

		public List<Edge> edges ()
		{
			return _edges;
		}
          
		public List<Vector2> region (Vector2 p)
		{
			Site site = _sitesIndexedByLocation [p];
			if (site == null) {
				return new List<Vector2> ();
			}
			return site.Region (_plotBounds);
		}

		// TODO: bug: if you call this before you call region(), something goes wrong :(
		public List<Vector2> neighborSitesForSite (Vector2 coord)
		{
			List<Vector2> points = new List<Vector2> ();
			Site site = _sitesIndexedByLocation [coord];
			if (site == null) {
				return points;
			}
			List<Site> sites = site.NeighborSites ();
			Site neighbor;
			for (int nIndex =0; nIndex<sites.Count; nIndex++) {
				neighbor = sites [nIndex];
				points.Add (neighbor.coord);
			}
			return points;
		}

		public List<Circle> circles ()
		{
			return _sites.circles ();
		}
		
		public List<LineSegment> voronoiBoundaryForSite (Vector2 coord)
		{
			return DelaunayHelpers.visibleLineSegments (DelaunayHelpers.selectEdgesForSitePoint (coord, _edges));
		}

		public List<LineSegment> delaunayLinesForSite (Vector2 coord)
		{
			return DelaunayHelpers.delaunayLinesForEdges (DelaunayHelpers.selectEdgesForSitePoint (coord, _edges));
		}
		
		public List<LineSegment> voronoiDiagram ()
		{
			return DelaunayHelpers.visibleLineSegments (_edges);
		}
		
		public List<LineSegment> delaunayTriangulation (/*BitmapData keepOutMask = null*/)
		{
			return DelaunayHelpers.delaunayLinesForEdges (DelaunayHelpers.selectNonIntersectingEdges (/*keepOutMask,*/_edges));
		}
		
		public List<LineSegment> hull ()
		{
			return DelaunayHelpers.delaunayLinesForEdges (HullEdges ());
		}
		
		private List<Edge> HullEdges ()
		{
			return _edges.FindAll (delegate (Edge edge) {
				return (edge.IsPartOfConvexHull ());
			});
		}

		public List<Vector2> hullPointsInOrder ()
		{
			List<Edge> hullEdges = HullEdges ();
			
			List<Vector2> points = new List<Vector2> ();
			if (hullEdges.Count == 0) {
				return points;
			}
			
			EdgeReorderer reorderer = new EdgeReorderer (hullEdges, VertexOrSite.SITE);
			hullEdges = reorderer.edges;
			List<Side> orientations = reorderer.edgeOrientations;
			reorderer.Dispose ();
			
			Side orientation;

			int n = hullEdges.Count;
			for (int i = 0; i < n; ++i) {
				Edge edge = hullEdges [i];
				orientation = orientations [i];
				points.Add (edge.Site (orientation).coord);
			}
			return points;
		}
		
		public List<LineSegment> spanningTree (KruskalType type = KruskalType.MINIMUM/*, BitmapData keepOutMask = null*/)
		{
			List<Edge> edges = DelaunayHelpers.selectNonIntersectingEdges (/*keepOutMask,*/_edges);
			List<LineSegment> segments = DelaunayHelpers.delaunayLinesForEdges (edges);
			return DelaunayHelpers.kruskal (segments, type);
		}

		public List<List<Vector2>> regions ()
		{
			return _sites.regions (_plotBounds);
		}
		
		public List<uint> siteColors (/*BitmapData referenceImage = null*/)
		{
			return _sites.siteColors (/*referenceImage*/);
		}
		
		/**
		 * 
		 * @param proximityMap a BitmapData whose regions are filled with the site index values; see PlanePointsCanvas::fillRegions()
		 * @param x
		 * @param y
		 * @return coordinates of nearest Site to (x, y)
		 * 
		 */
		public Nullable<Vector2> nearestSitePoint (/*BitmapData proximityMap,*/float x, float y)
		{
			return _sites.nearestSitePoint (/*proximityMap,*/x, y);
		}
		
		public List<Vector2> siteCoords ()
		{
			return _sites.siteCoords ();
		}

		private Site fortunesAlgorithm_bottomMostSite;
		private void fortunesAlgorithm ()
		{
			Site newSite, bottomSite, topSite, tempSite;
			Vertex v, vertex;
			Vector2 newintstar = Vector2.zero; //Because the compiler doesn't know that it will have a value - Julian
			Side leftRight;
			Halfedge lbnd, rbnd, llbnd, rrbnd, bisector;
			Edge edge;
			
			Rect dataBounds = _sites.getSitesBounds ();
			
			int sqrt_nsites = (int)(Mathf.Sqrt (_sites.Count + 4));
			HalfedgePriorityQueue heap = new HalfedgePriorityQueue (dataBounds.y, dataBounds.height, sqrt_nsites);
			EdgeList edgeList = new EdgeList (dataBounds.x, dataBounds.width, sqrt_nsites);
			List<Halfedge> halfEdges = new List<Halfedge> ();
			List<Vertex> vertices = new List<Vertex> ();
			
			fortunesAlgorithm_bottomMostSite = _sites.next ();
			newSite = _sites.next ();
			
			for (;;) {
				if (heap.empty () == false) {
					newintstar = heap.min ();
				}
			
				if (newSite != null 
					&& (heap.empty () || compareByYThenX (newSite, newintstar) < 0)) {
					/* new site is smallest */
					//trace("smallest: new site " + newSite);
					
					// Step 8:
					lbnd = edgeList.edgeListLeftNeighbor (newSite.coord);	// the Halfedge just to the left of newSite
					//trace("lbnd: " + lbnd);
					rbnd = lbnd.edgeListRightNeighbor;		// the Halfedge just to the right
					//trace("rbnd: " + rbnd);
					bottomSite = fortunesAlgorithm_rightRegion (lbnd);		// this is the same as leftRegion(rbnd)
					// this Site determines the region containing the new site
					//trace("new Site is in region of existing site: " + bottomSite);
					
					// Step 9:
					edge = Edge.createBisectingEdge (bottomSite, newSite);
					//trace("new edge: " + edge);
					_edges.Add (edge);
					
					bisector = Halfedge.Create (edge, Side.LEFT);
					halfEdges.Add (bisector);
					// inserting two Halfedges into edgeList constitutes Step 10:
					// insert bisector to the right of lbnd:
					edgeList.insert (lbnd, bisector);
					
					// first half of Step 11:
					if ((vertex = Vertex.Intersect (lbnd, bisector)) != null) {
						vertices.Add (vertex);
						heap.remove (lbnd);
						lbnd.vertex = vertex;
						lbnd.ystar = vertex.y + newSite.dist (vertex);
						heap.insert (lbnd);
					}
					
					lbnd = bisector;
					bisector = Halfedge.Create (edge, Side.RIGHT);
					halfEdges.Add (bisector);
					// second Halfedge for Step 10:
					// insert bisector to the right of lbnd:
					edgeList.insert (lbnd, bisector);
					
					// second half of Step 11:
					if ((vertex = Vertex.Intersect (bisector, rbnd)) != null) {
						vertices.Add (vertex);
						bisector.vertex = vertex;
						bisector.ystar = vertex.y + newSite.dist (vertex);
						heap.insert (bisector);	
					}
					
					newSite = _sites.next ();	
				} else if (heap.empty () == false) {
					/* intersection is smallest */
					lbnd = heap.extractMin ();
					llbnd = lbnd.edgeListLeftNeighbor;
					rbnd = lbnd.edgeListRightNeighbor;
					rrbnd = rbnd.edgeListRightNeighbor;
					bottomSite = fortunesAlgorithm_leftRegion (lbnd);
					topSite = fortunesAlgorithm_rightRegion (rbnd);
					// these three sites define a Delaunay triangle
					// (not actually using these for anything...)
					//_triangles.push(new Triangle(bottomSite, topSite, rightRegion(lbnd)));
					
					v = lbnd.vertex;
					v.SetIndex ();
					if (lbnd.leftRight == null) {
						Debug.LogError ("XXX: lbnd.leftRight is null but expected Side. Should make Nullable.");
					}
					if (rbnd.leftRight == null) {
						Debug.LogError ("XXX: rbnd.leftRight is null but expected Side. Should make Nullable.");
					}
					lbnd.edge.setVertex ((Side)lbnd.leftRight, v);
					rbnd.edge.setVertex ((Side)rbnd.leftRight, v);
					edgeList.remove (lbnd); 
					heap.remove (rbnd);
					edgeList.remove (rbnd); 
					leftRight = Side.LEFT;
					if (bottomSite.y > topSite.y) {
						tempSite = bottomSite;
						bottomSite = topSite;
						topSite = tempSite;
						leftRight = Side.RIGHT;
					}
					edge = Edge.createBisectingEdge (bottomSite, topSite);
					_edges.Add (edge);
					bisector = Halfedge.Create (edge, leftRight);
					halfEdges.Add (bisector);
					edgeList.insert (llbnd, bisector);
					edge.setVertex (SideHelper.other (leftRight), v);
					if ((vertex = Vertex.Intersect (llbnd, bisector)) != null) {
						vertices.Add (vertex);
						heap.remove (llbnd);
						llbnd.vertex = vertex;
						llbnd.ystar = vertex.y + bottomSite.dist (vertex);
						heap.insert (llbnd);
					}
					if ((vertex = Vertex.Intersect (bisector, rrbnd)) != null) {
						vertices.Add (vertex);
						bisector.vertex = vertex;
						bisector.ystar = vertex.y + bottomSite.dist (vertex);
						heap.insert (bisector);
					}
				} else {
					break;
				}
			}
			
			// heap should be empty now
			heap.Dispose ();
			edgeList.Dispose ();
			
			for (int hIndex = 0; hIndex<halfEdges.Count; hIndex++) {
				Halfedge halfEdge = halfEdges [hIndex];
				halfEdge.ReallyDispose ();
			}
			halfEdges.Clear ();
			
			// we need the vertices to clip the edges
			for (int eIndex = 0; eIndex<_edges.Count; eIndex++) {
				edge = _edges [eIndex];
				edge.ClipVertices (_plotBounds);
			}
			// but we don't actually ever use them again!
			for (int vIndex = 0; vIndex<vertices.Count; vIndex++) {
				vertex = vertices [vIndex];
				vertex.Dispose ();
			}
			vertices.Clear ();
		}

		private Site fortunesAlgorithm_leftRegion (Halfedge he)
		{
			Edge edge = he.edge;
			if (edge == null) {
				return fortunesAlgorithm_bottomMostSite;
			}
			if (he.leftRight == null) {
				Debug.LogError ("XXX: he.leftRight was null but expected Side. Should make Nullable");
			}
			return edge.Site ((Side)he.leftRight);
		}
		
		private Site fortunesAlgorithm_rightRegion (Halfedge he)
		{
			Edge edge = he.edge;
			if (edge == null) {
				return fortunesAlgorithm_bottomMostSite;
			}
			return edge.Site (SideHelper.other ((Side)he.leftRight));
		}

		public static int compareByYThenX (Site s1, Site s2) // TODO: Fix this (previously s2:*)
		{
			if (s1.y < s2.y)
				return -1;
			if (s1.y > s2.y)
				return 1;
			if (s1.x < s2.x)
				return -1;
			if (s1.x > s2.x)
				return 1;
			return 0;
		}

		public static int compareByYThenX (Site s1, Vector2 s2) // TODO: Fix this (previously s2:*)
		{
			if (s1.y < s2.y)
				return -1;
			if (s1.y > s2.y)
				return 1;
			if (s1.x < s2.x)
				return -1;
			if (s1.x > s2.x)
				return 1;
			return 0;
		}

	}
}