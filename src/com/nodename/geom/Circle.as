package com.nodename.geom
{
	import flash.geom.Point;
	
	public final class Circle extends Object
	{
		public var center:Point;
		public var radius:Number;
		
		public function Circle(centerX:Number, centerY:Number, radius:Number)
		{
			super();
			this.center = new Point(centerX, centerY);
			this.radius = radius;
		}
		
		public function toString():String
		{
			return "Circle (center: " + center + "; radius: " + radius + ")";
		}
		

		// If points p0, p1 and p2 are not collinear, they define a circle on the plane;
		// if the points are collinear, returns null.
		public static function circleThroughThreePoints(p0:Point, p1:Point, p2:Point):Circle
		{
			var tmpObj:Object = new Object();
			var centerX:Number;
			var centerY:Number;
			var radius:Number;
		 
			var bisect1:Object = findBisector(p0, p1);
			var bisect2:Object = findBisector(p0, p2);
		 
			if (lineLineIntersection(bisect1.midPoint, bisect1.bisectDir, bisect2.midPoint, bisect2.bisectDir, tmpObj) == 1)
			{
				radius = new Point(p0.x - tmpObj.point1.x, p0.y - tmpObj.point1.y).length;
				centerX = tmpObj.point1.x;
				centerY = tmpObj.point1.y;
				return new Circle(centerX, centerY, radius);
			} 
		 	return null;
		}
		 
		// find the bisector of a line segment defined on a couple points
		// returns an object with two points, the mid point of the line segment
		// and the direction or slope of the bisector
		private static function findBisector(p0:Point, p1:Point):Object
		{
			var vec01:Point = new Point(p1.x - p0.x, p1.y - p0.y);
		 
			var retObj:Object = new Object();
			retObj.midPoint = new Point(p0.x + 0.5 * vec01.x, p0.y + 0.5 * vec01.y);
			retObj.bisectDir = new Point(-vec01.y, vec01.x);
		 
			return retObj;	
		}
		 
		// return value 0 = lines don't intersect
		// return value 1 = lines intersect at single point, return object has field point1 with intersection
		// return value 2 = lines are the same
		private static function lineLineIntersection(p0:Point, d0:Point, p1:Point, d1:Point, retObj:Object):int
		{
			var E:Point = new Point(p1.x - p0.x, p1.y - p0.y);
			var kross:Number = (d0.x * d1.y) - (d0.y * d1.x);
			var sqrKross:Number = kross * kross;
			var sqrLen0:Number = (d0.x * d0.x) + (d0.y * d0.y);
			var sqrLen1:Number = (d1.x * d1.x) + (d1.y * d1.y);
			var epsilon:Number = 0.005;
			var sqrEpsilon:Number = epsilon * epsilon;
		 
			if (sqrKross > (sqrEpsilon * sqrLen0 * sqrLen1))
			{
				// lines of segment are not parallel
				var s:Number = ((E.x * d1.y) - (E.y * d1.x)) / kross;
		 
				// intersection of lines is a point
				retObj.point1 = new Point(p0.x + (s * d0.x), p0.y + (s * d0.y));
				return 1;
			}
		 
			// lines of segments are parallel
			var sqrLenE:Number = (E.x * E.x) + (E.y * E.y);
			kross = (E.x * d0.y) - (E.y * d0.x);
			sqrKross = kross * kross;
			if (sqrKross > (sqrEpsilon * sqrLen0 * sqrLenE))
			{
				// lines are parallel but not the same
				return 0;
			}
		 
			// Lines are the same. 
			return 2;
		}
	}
}