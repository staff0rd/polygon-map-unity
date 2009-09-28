package com.nodename.Delaunay
{
	import __AS3__.vec.Vector;
	import com.nodename.geom.LineSegment;
	
	internal function delaunayLinesForEdges(edges:Vector.<Edge>):Vector.<LineSegment>
	{
		var segments:Vector.<LineSegment> = new Vector.<LineSegment>();
		for each (var edge:Edge in edges)
		{
			segments.push(edge.delaunayLine());
		}
		return segments;
	}
}
	
