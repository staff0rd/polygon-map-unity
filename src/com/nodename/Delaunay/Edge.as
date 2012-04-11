package com.nodename.Delaunay
{
	import com.nodename.geom.LineSegment;
	
	import flash.display.BitmapData;
	import flash.display.CapsStyle;
	import flash.display.Graphics;
	import flash.display.LineScaleMode;
	import flash.display.Sprite;
	import flash.geom.Point;
	import flash.geom.Rectangle;
	import flash.utils.Dictionary;
	
	/**
	 * The line segment connecting the two Sites is part of the Delaunay triangulation;
	 * the line segment connecting the two Vertices is part of the Voronoi diagram
	 * @author ashaw
	 * 
	 */
	public final class Edge
	{
		private static var _pool:Vector.<Edge> = new Vector.<Edge>();

		/**
		 * This is the only way to create a new Edge 
		 * @param site0
		 * @param site1
		 * @return 
		 * 
		 */
		internal static function createBisectingEdge(site0:Site, site1:Site):Edge
		{
			var dx:Number, dy:Number, absdx:Number, absdy:Number;
			var a:Number, b:Number, c:Number;
		
			dx = site1.x - site0.x;
			dy = site1.y - site0.y;
			absdx = dx > 0 ? dx : -dx;
			absdy = dy > 0 ? dy : -dy;
			c = site0.x * dx + site0.y * dy + (dx * dx + dy * dy) * 0.5;
			if (absdx > absdy)
			{
				a = 1.0; b = dy/dx; c /= dx;
			}
			else
			{
				b = 1.0; a = dx/dy; c /= dy;
			}
			
			var edge:Edge = Edge.create();
		
			edge.leftSite = site0;
			edge.rightSite = site1;
			site0.addEdge(edge);
			site1.addEdge(edge);
			
			edge._leftVertex = null;
			edge._rightVertex = null;
			
			edge.a = a; edge.b = b; edge.c = c;
			//trace("createBisectingEdge: a ", edge.a, "b", edge.b, "c", edge.c);
			
			return edge;
		}

		private static function create():Edge
		{
			var edge:Edge;
			if (_pool.length > 0)
			{
				edge = _pool.pop();
				edge.init();
			}
			else
			{
				edge = new Edge(PrivateConstructorEnforcer);
			}
			return edge;
		}
		
		private static const LINESPRITE:Sprite = new Sprite();
		private static const GRAPHICS:Graphics = LINESPRITE.graphics;
		
		private var _delaunayLineBmp:BitmapData;
		internal function get delaunayLineBmp():BitmapData
		{
			if (!_delaunayLineBmp)
			{
				_delaunayLineBmp = makeDelaunayLineBmp();
			}
			return _delaunayLineBmp;
		}
		
		// making this available to Voronoi; running out of memory in AIR so I cannot cache the bmp
		internal function makeDelaunayLineBmp():BitmapData
		{
			var p0:Point = leftSite.coord;
			var p1:Point = rightSite.coord;
			
			GRAPHICS.clear();
			// clear() resets line style back to undefined!
			GRAPHICS.lineStyle(0, 0, 1.0, false, LineScaleMode.NONE, CapsStyle.NONE);
			GRAPHICS.moveTo(p0.x, p0.y);
			GRAPHICS.lineTo(p1.x, p1.y);
						
			var w:int = int(Math.ceil(Math.max(p0.x, p1.x)));
			if (w < 1)
			{
				w = 1;
			}
			var h:int = int(Math.ceil(Math.max(p0.y, p1.y)));
			if (h < 1)
			{
				h = 1;
			}
			var bmp:BitmapData = new BitmapData(w, h, true, 0);
			bmp.draw(LINESPRITE);
			return bmp;
		}

		public function delaunayLine():LineSegment
		{
			// draw a line connecting the input Sites for which the edge is a bisector:
			return new LineSegment(leftSite.coord, rightSite.coord);
		}

                public function voronoiEdge():LineSegment
                {
                  if (!visible) return new LineSegment(null, null);
                  return new LineSegment(_clippedVertices[LR.LEFT],
                                         _clippedVertices[LR.RIGHT]);
                }

		private static var _nedges:int = 0;
		
		internal static const DELETED:Edge = new Edge(PrivateConstructorEnforcer);
		
		// the equation of the edge: ax + by = c
		internal var a:Number, b:Number, c:Number;
		
		// the two Voronoi vertices that the edge connects
		//		(if one of them is null, the edge extends to infinity)
		private var _leftVertex:Vertex;
		internal function get leftVertex():Vertex
		{
			return _leftVertex;
		}
		private var _rightVertex:Vertex;
		internal function get rightVertex():Vertex
		{
			return _rightVertex;
		}
		internal function vertex(leftRight:LR):Vertex
		{
			return (leftRight == LR.LEFT) ? _leftVertex : _rightVertex;
		}
		internal function setVertex(leftRight:LR, v:Vertex):void
		{
			if (leftRight == LR.LEFT)
			{
				_leftVertex = v;
			}
			else
			{
				_rightVertex = v;
			}
		}
		
		internal function isPartOfConvexHull():Boolean
		{
			return (_leftVertex == null || _rightVertex == null);
		}
		
		public function sitesDistance():Number
		{
			return Point.distance(leftSite.coord, rightSite.coord);
		}
		
		public static function compareSitesDistances_MAX(edge0:Edge, edge1:Edge):Number
		{
			var length0:Number = edge0.sitesDistance();
			var length1:Number = edge1.sitesDistance();
			if (length0 < length1)
			{
				return 1;
			}
			if (length0 > length1)
			{
				return -1;
			}
			return 0;
		}
		
		public static function compareSitesDistances(edge0:Edge, edge1:Edge):Number
		{
			return - compareSitesDistances_MAX(edge0, edge1);
		}
		
		// Once clipVertices() is called, this Dictionary will hold two Points
		// representing the clipped coordinates of the left and right ends...
		private var _clippedVertices:Dictionary;
		internal function get clippedEnds():Dictionary
		{
			return _clippedVertices;
		}
		// unless the entire Edge is outside the bounds.
		// In that case visible will be false:
		internal function get visible():Boolean
		{
			return _clippedVertices != null;
		}
		
		// the two input Sites for which this Edge is a bisector:
		private var _sites:Dictionary;
		internal function set leftSite(s:Site):void
		{
			_sites[LR.LEFT] = s;
		}
		internal function get leftSite():Site
		{
			return _sites[LR.LEFT];
		}
		internal function set rightSite(s:Site):void
		{
			_sites[LR.RIGHT] = s;
		}
		internal function get rightSite():Site
		{
			return _sites[LR.RIGHT] as Site;
		}
		internal function site(leftRight:LR):Site
		{
			return _sites[leftRight] as Site;
		}
		
		private var _edgeIndex:int;
		
		public function dispose():void
		{
			if (_delaunayLineBmp)
			{
				_delaunayLineBmp.dispose();
				_delaunayLineBmp = null;
			}
			_leftVertex = null;
			_rightVertex = null;
			if (_clippedVertices)
			{
				_clippedVertices[LR.LEFT] = null;
				_clippedVertices[LR.RIGHT] = null;
				_clippedVertices = null;
			}
			_sites[LR.LEFT] = null;
			_sites[LR.RIGHT] = null;
			_sites = null;
			
			_pool.push(this);
		}

		public function Edge(lock:Class)
		{
			if (lock != PrivateConstructorEnforcer)
			{
				throw new Error("Edge: constructor is private");
			}
			
			_edgeIndex = _nedges++;
			init();
		}
		
		private function init():void
		{	
			_sites = new Dictionary(true);
		}
		
		public function toString():String
		{
			return "Edge " + _edgeIndex + "; sites " + _sites[LR.LEFT] + ", " + _sites[LR.RIGHT]
					+ "; endVertices " + (_leftVertex ? _leftVertex.vertexIndex : "null") + ", "
					 + (_rightVertex ? _rightVertex.vertexIndex : "null") + "::";
		}

		/**
		 * Set _clippedVertices to contain the two ends of the portion of the Voronoi edge that is visible
		 * within the bounds.  If no part of the Edge falls within the bounds, leave _clippedVertices null. 
		 * @param bounds
		 * 
		 */
		internal function clipVertices(bounds:Rectangle):void
		{
			var xmin:Number = bounds.x;
			var ymin:Number = bounds.y;
			var xmax:Number = bounds.right;
			var ymax:Number = bounds.bottom;
			
			var vertex0:Vertex, vertex1:Vertex;
			var x0:Number, x1:Number, y0:Number, y1:Number;
			
			if (a == 1.0 && b >= 0.0)
			{
				vertex0 = _rightVertex;
				vertex1 = _leftVertex;
			}
			else 
			{
				vertex0 = _leftVertex;
				vertex1 = _rightVertex;
			}
		
			if (a == 1.0)
			{
				y0 = ymin;
				if (vertex0 != null && vertex0.y > ymin)
				{
					 y0 = vertex0.y;
				}
				if (y0 > ymax)
				{
					return;
				}
				x0 = c - b * y0;
				
				y1 = ymax;
				if (vertex1 != null && vertex1.y < ymax)
				{
					y1 = vertex1.y;
				}
				if (y1 < ymin)
				{
					return;
				}
				x1 = c - b * y1;
				
				if ((x0 > xmax && x1 > xmax) || (x0 < xmin && x1 < xmin))
				{
					return;
				}
				
				if (x0 > xmax)
				{
					x0 = xmax; y0 = (c - x0)/b;
				}
				else if (x0 < xmin)
				{
					x0 = xmin; y0 = (c - x0)/b;
				}
				
				if (x1 > xmax)
				{
					x1 = xmax; y1 = (c - x1)/b;
				}
				else if (x1 < xmin)
				{
					x1 = xmin; y1 = (c - x1)/b;
				}
			}
			else
			{
				x0 = xmin;
				if (vertex0 != null && vertex0.x > xmin)
				{
					x0 = vertex0.x;
				}
				if (x0 > xmax)
				{
					return;
				}
				y0 = c - a * x0;
				
				x1 = xmax;
				if (vertex1 != null && vertex1.x < xmax)
				{
					x1 = vertex1.x;
				}
				if (x1 < xmin)
				{
					return;
				}
				y1 = c - a * x1;
				
				if ((y0 > ymax && y1 > ymax) || (y0 < ymin && y1 < ymin))
				{
					return;
				}
				
				if (y0 > ymax)
				{
					y0 = ymax; x0 = (c - y0)/a;
				}
				else if (y0 < ymin)
				{
					y0 = ymin; x0 = (c - y0)/a;
				}
				
				if (y1 > ymax)
				{
					y1 = ymax; x1 = (c - y1)/a;
				}
				else if (y1 < ymin)
				{
					y1 = ymin; x1 = (c - y1)/a;
				}
			}

			_clippedVertices = new Dictionary(true);
			if (vertex0 == _leftVertex)
			{
				_clippedVertices[LR.LEFT] = new Point(x0, y0);
				_clippedVertices[LR.RIGHT] = new Point(x1, y1);
			}
			else
			{
				_clippedVertices[LR.RIGHT] = new Point(x0, y0);
				_clippedVertices[LR.LEFT] = new Point(x1, y1);
			}
		}

	}
}

class PrivateConstructorEnforcer {}