using UnityEngine;
using System.Collections.Generic;
using Delaunay.Geom;

namespace Delaunay
{	
	List<LineSegment> visibleLineSegments(List<Edge> edges)
	{
		List<LineSegment> segments = new List<LineSegment>();
	
		for (int i = 0; i<edges.Count; i++)
		{
			Edge edge = edges[i];
			if (edge.visible)
			{
				Vector2 p1 = edge.clippedEnds[LR.LEFT];
				Vector2 p2 = edge.clippedEnds[LR.RIGHT];
				segments.Add(new LineSegment(p1, p2));
			}
		}
		
		return segments;
	}
}