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


package com.nodename.Delaunay
{
	import com.nodename.geom.Circle;
	import com.nodename.geom.LineSegment;
	
	import flash.display.BitmapData;
	import flash.geom.Point;
	import flash.geom.Rectangle;
	import flash.utils.Dictionary;
	
	public final class Voronoi
	{
		private var _sites:SiteList;
		private var _sitesIndexedByLocation:Dictionary;
		private var _triangles:Vector.<Triangle>;
		private var _edges:Vector.<Edge>;

		
		// TODO generalize this so it doesn't have to be a rectangle;
		// then we can make the fractal voronois-within-voronois
		private var _plotBounds:Rectangle;
		public function get plotBounds():Rectangle
		{
			return _plotBounds;
		}
		
		public function dispose():void
		{
			var i:int, n:int;
			if (_sites)
			{
				_sites.dispose();
				_sites = null;
			}
			if (_triangles)
			{
				n = _triangles.length;
				for (i = 0; i < n; ++i)
				{
					_triangles[i].dispose();
				}
				_triangles.length = 0;
				_triangles = null;
			}
			if (_edges)
			{
				n = _edges.length;
				for (i = 0; i < n; ++i)
				{
					_edges[i].dispose();
				}
				_edges.length = 0;
				_edges = null;
			}
			_plotBounds = null;
			_sitesIndexedByLocation = null;
		}
		
		public function Voronoi(points:Vector.<Point>, colors:Vector.<uint>, plotBounds:Rectangle)
		{
			_sites = new SiteList();
			_sitesIndexedByLocation = new Dictionary(true);
			addSites(points, colors);
			_plotBounds = plotBounds;
			_triangles = new Vector.<Triangle>();
			_edges = new Vector.<Edge>();
			fortunesAlgorithm();
		}
		
		private function addSites(points:Vector.<Point>, colors:Vector.<uint>):void
		{
			var length:uint = points.length;
			for (var i:uint = 0; i < length; ++i)
			{
				addSite(points[i], colors ? colors[i] : 0, i);
			}
		}
		
		private function addSite(p:Point, color:uint, index:int):void
		{
			var weight:Number = Math.random() * 100;
			var site:Site = Site.create(p, index, weight, color);
			_sites.push(site);
			_sitesIndexedByLocation[p] = site;
		}
		
		public function region(p:Point):Vector.<Point>
		{
			var site:Site = _sitesIndexedByLocation[p];
			if (!site)
			{
				return null;
			}
			return site.region(_plotBounds);
		}

		public function circles():Vector.<Circle>
		{
			return _sites.circles();
		}
		
		public function voronoiBoundaryForSite(coord:Point):Vector.<LineSegment>
		{
			return visibleLineSegments(selectEdgesForSitePoint(coord, _edges));
		}

		public function delaunayLinesForSite(coord:Point):Vector.<LineSegment>
		{
			return delaunayLinesForEdges(selectEdgesForSitePoint(coord, _edges));
		}
		
		public function voronoiDiagram():Vector.<LineSegment>
		{
			return visibleLineSegments(_edges);
		}
		
		public function delaunayTriangulation(keepOutMask:BitmapData = null):Vector.<LineSegment>
		{
			return delaunayLinesForEdges(selectNonIntersectingEdges(keepOutMask, _edges));
		}
		
		public function hull():Vector.<LineSegment>
		{
			return delaunayLinesForEdges(hullEdges());
		}
		
		private function hullEdges():Vector.<Edge>
		{
			return _edges.filter(myTest);
		
			function myTest(edge:Edge, index:int, vector:Vector.<Edge>):Boolean
			{
				return (edge.isPartOfConvexHull());
			}
		}

		public function hullPointsInOrder():Vector.<Point>
		{
			var hullEdges:Vector.<Edge> = hullEdges();
			
			var points:Vector.<Point> = new Vector.<Point>();
			if (hullEdges.length == 0)
			{
				return points;
			}
			
			var reorderer:EdgeReorderer = new EdgeReorderer(hullEdges, Site);
			hullEdges = reorderer.edges;
			var orientations:Vector.<LR> = reorderer.edgeOrientations;
			reorderer.dispose();
			
			var orientation:LR;

			var n:int = hullEdges.length;
			for (var i:int = 0; i < n; ++i)
			{
				var edge:Edge = hullEdges[i];
				orientation = orientations[i];
				points.push(edge.site(orientation).coord);
			}
			return points;
		}
		
		public function spanningTree(type:String = "minimum", keepOutMask:BitmapData = null):Vector.<LineSegment>
		{
			var edges:Vector.<Edge> = selectNonIntersectingEdges(keepOutMask, _edges);
			var segments:Vector.<LineSegment> = delaunayLinesForEdges(edges);
			return kruskal(segments, type);
		}

		public function regions():Vector.<Vector.<Point>>
		{
			return _sites.regions(_plotBounds);
		}
		
		public function siteColors(referenceImage:BitmapData = null):Vector.<uint>
		{
			return _sites.siteColors(referenceImage);
		}
		
		/**
		 * 
		 * @param proximityMap a BitmapData whose regions are filled with the site index values; see PlanePointsCanvas::fillRegions()
		 * @param x
		 * @param y
		 * @return coordinates of nearest Site to (x, y)
		 * 
		 */
		public function nearestSitePoint(proximityMap:BitmapData, x:Number, y:Number):Point
		{
			return _sites.nearestSitePoint(proximityMap, x, y);
		}
		
		public function siteCoords():Vector.<Point>
		{
			return _sites.siteCoords();
		}

		private function fortunesAlgorithm():void
		{
			var newSite:Site, bottomSite:Site, topSite:Site, tempSite:Site;
			var v:Vertex, vertex:Vertex;
			var newintstar:Point;
			var leftRight:LR;
			var lbnd:Halfedge, rbnd:Halfedge, llbnd:Halfedge, rrbnd:Halfedge, bisector:Halfedge;
			var edge:Edge;
			
			var dataBounds:Rectangle = _sites.getSitesBounds();
			
			var sqrt_nsites:int = int(Math.sqrt(_sites.length + 4));
			var heap:HalfedgePriorityQueue = new HalfedgePriorityQueue(dataBounds.y, dataBounds.height, sqrt_nsites);
			var edgeList:EdgeList = new EdgeList(dataBounds.x, dataBounds.width, sqrt_nsites);
			var halfEdges:Vector.<Halfedge> = new Vector.<Halfedge>();
			var vertices:Vector.<Vertex> = new Vector.<Vertex>();
			
			var bottomMostSite:Site = _sites.next();
			newSite = _sites.next();
			
			for (;;)
			{
				if (heap.empty() == false)
				{
					newintstar = heap.min();
				}
			
				if (newSite != null 
				&&  (heap.empty() || compareByYThenX(newSite, newintstar) < 0))
				{
					/* new site is smallest */
					//trace("smallest: new site " + newSite);
					
					// Step 8:
					lbnd = edgeList.edgeListLeftNeighbor(newSite.coord);	// the Halfedge just to the left of newSite
					//trace("lbnd: " + lbnd);
					rbnd = lbnd.edgeListRightNeighbor;		// the Halfedge just to the right
					//trace("rbnd: " + rbnd);
					bottomSite = rightRegion(lbnd);		// this is the same as leftRegion(rbnd)
					// this Site determines the region containing the new site
					//trace("new Site is in region of existing site: " + bottomSite);
					
					// Step 9:
					edge = Edge.createBisectingEdge(bottomSite, newSite);
					//trace("new edge: " + edge);
					_edges.push(edge);
					
					bisector = Halfedge.create(edge, LR.LEFT);
					halfEdges.push(bisector);
					// inserting two Halfedges into edgeList constitutes Step 10:
					// insert bisector to the right of lbnd:
					edgeList.insert(lbnd, bisector);
					
					// first half of Step 11:
					if ((vertex = Vertex.intersect(lbnd, bisector)) != null) 
					{
						vertices.push(vertex);
						heap.remove(lbnd);
						lbnd.vertex = vertex;
						lbnd.ystar = vertex.y + newSite.dist(vertex);
						heap.insert(lbnd);
					}
					
					lbnd = bisector;
					bisector = Halfedge.create(edge, LR.RIGHT);
					halfEdges.push(bisector);
					// second Halfedge for Step 10:
					// insert bisector to the right of lbnd:
					edgeList.insert(lbnd, bisector);
					
					// second half of Step 11:
					if ((vertex = Vertex.intersect(bisector, rbnd)) != null)
					{
						vertices.push(vertex);
						bisector.vertex = vertex;
						bisector.ystar = vertex.y + newSite.dist(vertex);
						heap.insert(bisector);	
					}
					
					newSite = _sites.next();	
				}
				else if (heap.empty() == false) 
				{
					/* intersection is smallest */
					lbnd = heap.extractMin();
					llbnd = lbnd.edgeListLeftNeighbor;
					rbnd = lbnd.edgeListRightNeighbor;
					rrbnd = rbnd.edgeListRightNeighbor;
					bottomSite = leftRegion(lbnd);
					topSite = rightRegion(rbnd);
					// these three sites define a Delaunay triangle
					// (not actually using these for anything...)
					//_triangles.push(new Triangle(bottomSite, topSite, rightRegion(lbnd)));
					
					v = lbnd.vertex;
					v.setIndex();
					lbnd.edge.setVertex(lbnd.leftRight, v);
					rbnd.edge.setVertex(rbnd.leftRight, v);
					edgeList.remove(lbnd); 
					heap.remove(rbnd);
					edgeList.remove(rbnd); 
					leftRight = LR.LEFT;
					if (bottomSite.y > topSite.y)
					{
						tempSite = bottomSite; bottomSite = topSite; topSite = tempSite; leftRight = LR.RIGHT;
					}
					edge = Edge.createBisectingEdge(bottomSite, topSite);
					_edges.push(edge);
					bisector = Halfedge.create(edge, leftRight);
					halfEdges.push(bisector);
					edgeList.insert(llbnd, bisector);
					edge.setVertex(LR.other(leftRight), v);
					if ((vertex = Vertex.intersect(llbnd, bisector)) != null)
					{
						vertices.push(vertex);
						heap.remove(llbnd);
						llbnd.vertex = vertex;
						llbnd.ystar = vertex.y + bottomSite.dist(vertex);
						heap.insert(llbnd);
					}
					if ((vertex = Vertex.intersect(bisector, rrbnd)) != null)
					{
						vertices.push(vertex);
						bisector.vertex = vertex;
						bisector.ystar = vertex.y + bottomSite.dist(vertex);
						heap.insert(bisector);
					}
				}
				else
				{
					break;
				}
			}
			
			// heap should be empty now
			heap.dispose();
			edgeList.dispose();
			
			for each (var halfEdge:Halfedge in halfEdges)
			{
				halfEdge.reallyDispose();
			}
			halfEdges.length = 0;
			
			// we need the vertices to clip the edges
			for each (edge in _edges)
			{
				edge.clipVertices(_plotBounds);
			}
			// but we don't actually ever use them again!
			for each (vertex in vertices)
			{
				vertex.dispose();
			}
			vertices.length = 0;
			
			function leftRegion(he:Halfedge):Site
			{
				var edge:Edge = he.edge;
				if (edge == null)
				{
					return bottomMostSite;
				}
				return edge.site(he.leftRight);
			}
			
			function rightRegion(he:Halfedge):Site
			{
				var edge:Edge = he.edge;
				if (edge == null)
				{
					return bottomMostSite;
				}
				return edge.site(LR.other(he.leftRight));
			}
		}

		internal static function compareByYThenX(s1:Site, s2:*):Number
		{
			if (s1.y < s2.y) return -1;
			if (s1.y > s2.y) return 1;
			if (s1.x < s2.x) return -1;
			if (s1.x > s2.x) return 1;
			return 0;
		}

	}
}