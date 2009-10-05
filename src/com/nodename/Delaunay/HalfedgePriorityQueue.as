package com.nodename.Delaunay
{
	import flash.geom.Point;
	
	internal final class HalfedgePriorityQueue // also known as heap
	{
		private var _hash:Vector.<Halfedge>;
		private var _count:int;
		private var _minBucket:int;
		private var _hashsize:int;
		
		private var _ymin:Number;
		private var _deltay:Number;
		
		public function HalfedgePriorityQueue(ymin:Number, deltay:Number, sqrt_nsites:int)
		{
			_ymin = ymin;
			_deltay = deltay;
			_hashsize = 4 * sqrt_nsites;
			initialize();
		}
		
		public function dispose():void
		{
			// get rid of dummies
			for (var i:int = 0; i < _hashsize; ++i)
			{
				_hash[i].dispose();
				_hash[i] = null;
			}
			_hash = null;
		}

		private function initialize():void
		{
			var i:int;
		
			_count = 0;
			_minBucket = 0;
			_hash = new Vector.<Halfedge>(_hashsize);
			// dummy Halfedge at the top of each hash
			for (i = 0; i < _hashsize; ++i)
			{
				_hash[i] = Halfedge.createDummy();
				_hash[i].nextInPriorityQueue = null;
			}
		}

		public function insert(halfEdge:Halfedge):void
		{
			var previous:Halfedge, next:Halfedge;
			var insertionBucket:int = bucket(halfEdge);
			if (insertionBucket < _minBucket)
			{
				_minBucket = insertionBucket;
			}
			previous = _hash[insertionBucket];
			while ((next = previous.nextInPriorityQueue) != null
			&&     (halfEdge.ystar  > next.ystar || (halfEdge.ystar == next.ystar && halfEdge.vertex.x > next.vertex.x)))
			{
				previous = next;
			}
			halfEdge.nextInPriorityQueue = previous.nextInPriorityQueue; 
			previous.nextInPriorityQueue = halfEdge;
			++_count;
		}

		public function remove(halfEdge:Halfedge):void
		{
			var previous:Halfedge;
			var removalBucket:int = bucket(halfEdge);
			
			if (halfEdge.vertex != null)
			{
				previous = _hash[removalBucket];
				while (previous.nextInPriorityQueue != halfEdge)
				{
					previous = previous.nextInPriorityQueue;
				}
				previous.nextInPriorityQueue = halfEdge.nextInPriorityQueue;
				_count--;
				halfEdge.vertex = null;
				halfEdge.nextInPriorityQueue = null;
				halfEdge.dispose();
			}
		}

		private function bucket(halfEdge:Halfedge):int
		{
			var theBucket:int = (halfEdge.ystar - _ymin)/_deltay * _hashsize;
			if (theBucket < 0) theBucket = 0;
			if (theBucket >= _hashsize) theBucket = _hashsize - 1;
			return theBucket;
		}
		
		private function isEmpty(bucket:int):Boolean
		{
			return (_hash[bucket].nextInPriorityQueue == null);
		}
		
		/**
		 * move _minBucket until it contains an actual Halfedge (not just the dummy at the top); 
		 * 
		 */
		private function adjustMinBucket():void
		{
			while (_minBucket < _hashsize - 1 && isEmpty(_minBucket))
			{
				++_minBucket;
			}
		}

		public function empty():Boolean
		{
			return _count == 0;
		}

		/**
		 * @return coordinates of the Halfedge's vertex in V*, the transformed Voronoi diagram
		 * 
		 */
		public function min():Point
		{
			adjustMinBucket();
			var answer:Halfedge = _hash[_minBucket].nextInPriorityQueue;
			return new Point(answer.vertex.x, answer.ystar);
		}

		/**
		 * remove and return the min Halfedge
		 * @return 
		 * 
		 */
		public function extractMin():Halfedge
		{
			var answer:Halfedge;
		
			// get the first real Halfedge in _minBucket
			answer = _hash[_minBucket].nextInPriorityQueue;
			
			_hash[_minBucket].nextInPriorityQueue = answer.nextInPriorityQueue;
			_count--;
			answer.nextInPriorityQueue = null;
			
			return answer;
		}

	}
}