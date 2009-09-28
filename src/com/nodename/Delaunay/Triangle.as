package com.nodename.Delaunay
{
	import __AS3__.vec.Vector;
	
	public final class Triangle
	{
		private var _sites:Vector.<Site>;
		public function get sites():Vector.<Site>
		{
			return _sites;
		}
		
		public function Triangle(a:Site, b:Site, c:Site)
		{
			_sites = Vector.<Site>([ a, b, c ]);
		}
		
		public function dispose():void
		{
			_sites.length = 0;
			_sites = null;
		}

	}
}