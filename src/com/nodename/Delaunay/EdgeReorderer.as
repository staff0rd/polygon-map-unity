package com.nodename.Delaunay
{
	
	
	internal final class EdgeReorderer
	{
		private var _edges:Vector.<Edge>;
		private var _edgeOrientations:Vector.<LR>;
		public function get edges():Vector.<Edge>
		{
			return _edges;
		}
		public function get edgeOrientations():Vector.<LR>
		{
			return _edgeOrientations;
		}
		
		public function EdgeReorderer(origEdges:Vector.<Edge>, criterion:Class)
		{
			if (criterion != Vertex && criterion != Site)
			{
				throw new ArgumentError("Edges: criterion must be Vertex or Site");
			}
			_edges = new Vector.<Edge>();
			_edgeOrientations = new Vector.<LR>();
			if (origEdges.length > 0)
			{
				_edges = reorderEdges(origEdges, criterion);
			}
		}
		
		public function dispose():void
		{
			_edges = null;
			_edgeOrientations = null;
		}

		private function reorderEdges(origEdges:Vector.<Edge>, criterion:Class):Vector.<Edge>
		{
			var i:int;
			var j:int;
			var n:int = origEdges.length;
			var edge:Edge;
			// we're going to reorder the edges in order of traversal
			var done:Vector.<Boolean> = new Vector.<Boolean>(n, true);
			var nDone:int = 0;
			for each (var b:Boolean in done)
			{
				b = false;
			}
			var newEdges:Vector.<Edge> = new Vector.<Edge>();
			
			i = 0;
			edge = origEdges[i];
			newEdges.push(edge);
			_edgeOrientations.push(LR.LEFT);
			var firstPoint:ICoord = (criterion == Vertex) ? edge.leftVertex : edge.leftSite;
			var lastPoint:ICoord = (criterion == Vertex) ? edge.rightVertex : edge.rightSite;
			
			if (firstPoint == Vertex.VERTEX_AT_INFINITY || lastPoint == Vertex.VERTEX_AT_INFINITY)
			{
				return new Vector.<Edge>();
			}
			
			done[i] = true;
			++nDone;
			
			while (nDone < n)
			{
				for (i = 1; i < n; ++i)
				{
					if (done[i])
					{
						continue;
					}
					edge = origEdges[i];
					var leftPoint:ICoord = (criterion == Vertex) ? edge.leftVertex : edge.leftSite;
					var rightPoint:ICoord = (criterion == Vertex) ? edge.rightVertex : edge.rightSite;
					if (leftPoint == Vertex.VERTEX_AT_INFINITY || rightPoint == Vertex.VERTEX_AT_INFINITY)
					{
						return new Vector.<Edge>();
					}
					if (leftPoint == lastPoint)
					{
						lastPoint = rightPoint;
						_edgeOrientations.push(LR.LEFT);
						newEdges.push(edge);
						done[i] = true;
					}
					else if (rightPoint == firstPoint)
					{
						firstPoint = leftPoint;
						_edgeOrientations.unshift(LR.LEFT);
						newEdges.unshift(edge);
						done[i] = true;
					}
					else if (leftPoint == firstPoint)
					{
						firstPoint = rightPoint;
						_edgeOrientations.unshift(LR.RIGHT);
						newEdges.unshift(edge);
						done[i] = true;
					}
					else if (rightPoint == lastPoint)
					{
						lastPoint = leftPoint;
						_edgeOrientations.push(LR.RIGHT);
						newEdges.push(edge);
						done[i] = true;
					}
					if (done[i])
					{
						++nDone;
					}
				}
			}
			
			return newEdges;
		}

	}
}