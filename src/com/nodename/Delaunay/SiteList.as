package com.nodename.Delaunay
{
	import com.nodename.geom.Circle;
	import com.nodename.utils.IDisposable;
	
	import flash.display.BitmapData;
	import flash.geom.Point;
	import flash.geom.Rectangle;

	internal final class SiteList implements IDisposable
	{
		private var _sites:Vector.<Site>;
		private var _currentIndex:uint;
		
		private var _sorted:Boolean;
		
		public function SiteList()
		{
			_sites = new Vector.<Site>();
			_sorted = false;
		}
		
		public function dispose():void
		{
			if (_sites)
			{
				for each (var site:Site in _sites)
				{
					site.dispose();
				}
				_sites.length = 0;
				_sites = null;
			}
		}
		
		public function push(site:Site):uint
		{
			_sorted = false;
			return _sites.push(site);
		}
		
		public function get length():uint
		{
			return _sites.length;
		}
		
		public function next():Site
		{
			if (_sorted == false)
			{
				throw new Error("SiteList::next():  sites have not been sorted");
			}
			if (_currentIndex < _sites.length)
			{
				return _sites[_currentIndex++];
			}
			else
			{
				return null;
			}
		}

		internal function getSitesBounds():Rectangle
		{
			if (_sorted == false)
			{
				Site.sortSites(_sites);
				_currentIndex = 0;
				_sorted = true;
			}
			var xmin:Number, xmax:Number, ymin:Number, ymax:Number;
			if (_sites.length == 0)
			{
				return new Rectangle(0, 0, 0, 0);
			}
			xmin = Number.MAX_VALUE;
			xmax = Number.MIN_VALUE;
			for each (var site:Site in _sites)
			{
				if (site.x < xmin)
				{
					xmin = site.x;
				}
				if (site.x > xmax)
				{
					xmax = site.x;
				}
			}
			// here's where we assume that the sites have been sorted on y:
			ymin = _sites[0].y;
			ymax = _sites[_sites.length - 1].y;
			
			return new Rectangle(xmin, ymin, xmax - xmin, ymax - ymin);
		}

		public function siteColors(referenceImage:BitmapData = null):Vector.<uint>
		{
			var colors:Vector.<uint> = new Vector.<uint>();
			for each (var site:Site in _sites)
			{
				colors.push(referenceImage ? referenceImage.getPixel(site.x, site.y) : site.color);
			}
			return colors;
		}

		public function siteCoords():Vector.<Point>
		{
			var coords:Vector.<Point> = new Vector.<Point>();
			for each (var site:Site in _sites)
			{
				coords.push(site.coord);
			}
			return coords;
		}

		/**
		 * 
		 * @return the largest circle centered at each site that fits in its region;
		 * if the region is infinite, return a circle of radius 0.
		 * 
		 */
		public function circles():Vector.<Circle>
		{
			var circles:Vector.<Circle> = new Vector.<Circle>();
			for each (var site:Site in _sites)
			{
				var radius:Number = 0;
				var nearestEdge:Edge = site.nearestEdge();
				
				!nearestEdge.isPartOfConvexHull() && (radius = nearestEdge.sitesDistance() * 0.5);
				circles.push(new Circle(site.x, site.y, radius));
			}
			return circles;
		}

		public function regions(plotBounds:Rectangle):Vector.<Vector.<Point>>
		{
			var regions:Vector.<Vector.<Point>> = new Vector.<Vector.<Point>>();
			for each (var site:Site in _sites)
			{
				regions.push(site.region(plotBounds));
			}
			return regions;
		}

		/**
		 * 
		 * @param proximityMap a BitmapData whose regions are filled with the site index values; see PlanePointsCanvas::fillRegions()
		 * @param x
		 * @param y
		 * @return coordinates of nearest Site to (x, y)
		 * 
		 */
		public function nearestSitePoint(proximityMap:BitmapData, x:Number, y:Number):Point
		{
			var index:uint = proximityMap.getPixel(x, y);
			if (index > _sites.length - 1)
			{
				return null;
			}
			return _sites[index].coord;
		}
		
	}
}