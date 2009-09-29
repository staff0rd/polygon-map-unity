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

	}
}