package com.nodename.Delaunay
{
	import flash.geom.Point;
	
	internal final class Vertex extends Object implements ICoord
	{
		internal static const VERTEX_AT_INFINITY:Vertex = new Vertex(PrivateConstructorEnforcer, NaN, NaN);
		
		private static var _pool:Vector.<Vertex> = new Vector.<Vertex>();
		private static function create(x:Number, y:Number):Vertex
		{
			if (isNaN(x) || isNaN(y))
			{
				return VERTEX_AT_INFINITY;
			}
			if (_pool.length > 0)
			{
				return _pool.pop().init(x, y);
			}
			else
			{
				return new Vertex(PrivateConstructorEnforcer, x, y);
			}
		}


		private static var _nvertices:int = 0;
		
		private var _coord:Point;
		public function get coord():Point
		{
			return _coord;
		}
		private var _vertexIndex:int;
		public function get vertexIndex():int
		{
			return _vertexIndex;
		}
		
		public function Vertex(lock:Class, x:Number, y:Number)
		{
			if (lock != PrivateConstructorEnforcer)
			{
				throw new Error("Vertex constructor is private");
			}
			
			init(x, y);
		}
		
		private function init(x:Number, y:Number):Vertex
		{
			_coord = new Point(x, y);
			return this;
		}
		
		public function dispose():void
		{
			_coord = null;
			_pool.push(this);
		}
		
		public function setIndex():void
		{
			_vertexIndex = _nvertices++;
		}
		
		public function toString():String
		{
			return "Vertex (" + _vertexIndex + ")";
		}

		/**
		 * This is the only way to make a Vertex
		 * 
		 * @param halfedge0
		 * @param halfedge1
		 * @return 
		 * 
		 */
		public static function intersect(halfedge0:Halfedge, halfedge1:Halfedge):Vertex
		{
			var edge0:Edge, edge1:Edge, edge:Edge;
			var halfedge:Halfedge;
			var determinant:Number, intersectionX:Number, intersectionY:Number;
			var rightOfSite:Boolean;
		
			edge0 = halfedge0.edge;
			edge1 = halfedge1.edge;
			if (edge0 == null || edge1 == null)
			{
				return null;
			}
			if (edge0.rightSite == edge1.rightSite)
			{
				return null;
			}
		
			determinant = edge0.a * edge1.b - edge0.b * edge1.a;
			if (-1.0e-10 < determinant && determinant < 1.0e-10)
			{
				// the edges are parallel
				return null;
			}
		
			intersectionX = (edge0.c * edge1.b - edge1.c * edge0.b)/determinant;
			intersectionY = (edge1.c * edge0.a - edge0.c * edge1.a)/determinant;
		
			if (Voronoi.compareByYThenX(edge0.rightSite, edge1.rightSite) < 0)
			{
				halfedge = halfedge0; edge = edge0;
			}
			else
			{
				halfedge = halfedge1; edge = edge1;
			}
			rightOfSite = intersectionX >= edge.rightSite.x;
			if ((rightOfSite && halfedge.leftRight == LR.LEFT)
			||  (!rightOfSite && halfedge.leftRight == LR.RIGHT))
			{
				return null;
			}
		
			return Vertex.create(intersectionX, intersectionY);
		}
		
		public function get x():Number
		{
			return _coord.x;
		}
		public function get y():Number
		{
			return _coord.y;
		}
		
	}
}

class PrivateConstructorEnforcer {}