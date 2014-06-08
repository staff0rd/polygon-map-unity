package com.nodename.Delaunay
{
	import flash.geom.Point;
	import flash.display.BitmapData;
	
	internal function selectNonIntersectingEdges(keepOutMask:BitmapData, edgesToTest:Vector.<Edge>):Vector.<Edge>
	{
		if (keepOutMask == null)
		{
			return edgesToTest;
		}
		
		var zeroPoint:Point = new Point();
		return edgesToTest.filter(myTest);
		
		function myTest(edge:Edge, index:int, vector:Vector.<Edge>):Boolean
		{
			var delaunayLineBmp:BitmapData = edge.makeDelaunayLineBmp();
			var notIntersecting:Boolean = !(keepOutMask.hitTest(zeroPoint, 1, delaunayLineBmp, zeroPoint, 1));
			delaunayLineBmp.dispose();
			return notIntersecting;
		}
	}
}
	
