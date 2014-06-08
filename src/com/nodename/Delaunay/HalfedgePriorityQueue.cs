using UnityEngine;
using System.Collections.Generic;
using Delaunay.Utils;

namespace Delaunay
{
	
	internal sealed class HalfedgePriorityQueue: Utils.IDisposable // also known as heap
	{
		private List<Halfedge> _hash;
		private int _count;
		private int _minBucket;
		private int _hashsize;
		
		private float _ymin;
		private float _deltay;
		
		public HalfedgePriorityQueue (float ymin, float deltay, int sqrt_nsites)
		{
			_ymin = ymin;
			_deltay = deltay;
			_hashsize = 4 * sqrt_nsites;
			Initialize ();
		}
		
		public void Dispose ()
		{
			// get rid of dummies
			for (int i = 0; i < _hashsize; ++i) {
				_hash [i].Dispose ();
				_hash [i] = null;
			}
			_hash = null;
		}

		private void Initialize ()
		{
			int i;
		
			_count = 0;
			_minBucket = 0;
			_hash = new List<Halfedge> (_hashsize);
			// dummy Halfedge at the top of each hash
			for (i = 0; i < _hashsize; ++i) {
				_hash [i] = Halfedge.createDummy ();
				_hash [i].nextInPriorityQueue = null;
			}
		}

		public void insert (Halfedge halfEdge)
		{
			Halfedge previous, next;
			int insertionBucket = bucket (halfEdge);
			if (insertionBucket < _minBucket) {
				_minBucket = insertionBucket;
			}
			previous = _hash [insertionBucket];
			while ((next = previous.nextInPriorityQueue) != null
			&&     (halfEdge.ystar  > next.ystar || (halfEdge.ystar == next.ystar && halfEdge.vertex.x > next.vertex.x))) {
				previous = next;
			}
			halfEdge.nextInPriorityQueue = previous.nextInPriorityQueue; 
			previous.nextInPriorityQueue = halfEdge;
			++_count;
		}

		public void remove (Halfedge halfEdge)
		{
			Halfedge previous;
			int removalBucket = bucket (halfEdge);
			
			if (halfEdge.vertex != null) {
				previous = _hash [removalBucket];
				while (previous.nextInPriorityQueue != halfEdge) {
					previous = previous.nextInPriorityQueue;
				}
				previous.nextInPriorityQueue = halfEdge.nextInPriorityQueue;
				_count--;
				halfEdge.vertex = null;
				halfEdge.nextInPriorityQueue = null;
				halfEdge.Dispose ();
			}
		}

		private int bucket (Halfedge halfEdge)
		{
			int theBucket = (int)((halfEdge.ystar - _ymin) / _deltay * _hashsize);
			if (theBucket < 0)
				theBucket = 0;
			if (theBucket >= _hashsize)
				theBucket = _hashsize - 1;
			return theBucket;
		}
		
		private bool isEmpty (int bucket)
		{
			return (_hash [bucket].nextInPriorityQueue == null);
		}
		
		/**
		 * move _minBucket until it contains an actual Halfedge (not just the dummy at the top); 
		 * 
		 */
		private void adjustMinBucket ()
		{
			while (_minBucket < _hashsize - 1 && isEmpty(_minBucket)) {
				++_minBucket;
			}
		}

		public bool empty ()
		{
			return _count == 0;
		}

		/**
		 * @return coordinates of the Halfedge's vertex in V*, the transformed Voronoi diagram
		 * 
		 */
		public Vector2 min ()
		{
			adjustMinBucket ();
			Halfedge answer = _hash [_minBucket].nextInPriorityQueue;
			return new Vector2 (answer.vertex.x, answer.ystar);
		}

		/**
		 * remove and return the min Halfedge
		 * @return 
		 * 
		 */
		public Halfedge extractMin ()
		{
			Halfedge answer;
		
			// get the first real Halfedge in _minBucket
			answer = _hash [_minBucket].nextInPriorityQueue;
			
			_hash [_minBucket].nextInPriorityQueue = answer.nextInPriorityQueue;
			_count--;
			answer.nextInPriorityQueue = null;
			
			return answer;
		}

	}
}