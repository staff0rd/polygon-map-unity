using UnityEngine;
using System;
using System.Collections.Generic;
using Delaunay.Geo;
using Delaunay.LR;

namespace Delaunay
{	
	public static class DelaunayHelpers
	{
		public static List<LineSegment> visibleLineSegments (List<Edge> edges)
		{
			List<LineSegment> segments = new List<LineSegment> ();
			
			for (int i = 0; i<edges.Count; i++) {
				Edge edge = edges [i];
				if (edge.visible) {
					Nullable<Vector2> p1 = edge.clippedEnds [Side.LEFT];
					Nullable<Vector2> p2 = edge.clippedEnds [Side.RIGHT];
					segments.Add (new LineSegment (p1, p2));
				}
			}
			
			return segments;
		}
	}
}