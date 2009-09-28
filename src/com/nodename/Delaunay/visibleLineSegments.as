package com.nodename.Delaunay
{
	import __AS3__.vec.Vector;
	import com.nodename.geom.LineSegment;
	import flash.geom.Point;
	
	internal function visibleLineSegments(edges:Vector.<Edge>):Vector.<LineSegment>
	{
		var segments:Vector.<LineSegment> = new Vector.<LineSegment>();
	
		for each (var edge:Edge in edges)
		{
			if (edge.visible)
			{
				var p1:Point = edge.clippedEnds[LR.LEFT];
				var p2:Point = edge.clippedEnds[LR.RIGHT];
				segments.push(new LineSegment(p1, p2));
			}
		}
		
		return segments;
	}
}
	
