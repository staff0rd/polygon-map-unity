package com.nodename.Delaunay
{
	import __AS3__.vec.Vector;
	import flash.utils.Dictionary;
	import com.nodename.geom.LineSegment;
	
/**
*  Kruskal's spanning tree algorithm with union-find
 * Skiena: The Algorithm Design Manual, p. 196ff
 * Note: the sites are implied: they consist of the end points of the line segments
*/
	public function kruskal(lineSegments:Vector.<LineSegment>, type:String = "minimum"):Vector.<LineSegment>
	{
		var nodes:Dictionary = new Dictionary(true);
		var mst:Vector.<LineSegment> = new Vector.<LineSegment>();
		var nodePool:Vector.<Node> = Node.pool;
		
		switch (type)
		{
			// note that the compare functions are the reverse of what you'd expect
			// because (see below) we traverse the lineSegments in reverse order for speed
			case "maximum":
				lineSegments.sort(LineSegment.compareLengths);
				break;
			default:
				lineSegments.sort(LineSegment.compareLengths_MAX);
				break;
		}
		
		for (var i:int = lineSegments.length; --i > -1;)
		{
			var lineSegment:LineSegment = lineSegments[i];
			
			var node0:Node = nodes[lineSegment.p0];
			var rootOfSet0:Node;
			if (node0 == null)
			{
				node0 = nodePool.length > 0 ? nodePool.pop() : new Node();
				// intialize the node:
				rootOfSet0 = node0.parent = node0;
				node0.treeSize = 1;
			
				nodes[lineSegment.p0] = node0;
			}
			else
			{
				rootOfSet0 = find(node0);
			}
			
			var node1:Node = nodes[lineSegment.p1];
			var rootOfSet1:Node;
			if (node1 == null)
			{
				node1 = nodePool.length > 0 ? nodePool.pop() : new Node();
				// intialize the node:
				rootOfSet1 = node1.parent = node1;
				node1.treeSize = 1;
			
				nodes[lineSegment.p1] = node1;
			}
			else
			{
				rootOfSet1 = find(node1);
			}
			
			if (rootOfSet0 != rootOfSet1)	// nodes not in same set
			{
				mst.push(lineSegment);
				
				// merge the two sets:
				var treeSize0:int = rootOfSet0.treeSize;
				var treeSize1:int = rootOfSet1.treeSize;
				if (treeSize0 >= treeSize1)
				{
					// set0 absorbs set1:
					rootOfSet1.parent = rootOfSet0;
					rootOfSet0.treeSize += treeSize1;
				}
				else
				{
					// set1 absorbs set0:
					rootOfSet0.parent = rootOfSet1;
					rootOfSet1.treeSize += treeSize0;
				}
			}
		}
		
		for each (var node:Node in nodes)
		{
			nodePool.push(node);
		}
		
		return mst;
	}
}

function find(node:Node):Node
{
	if (node.parent == node)
	{
		return node;
	}
	else
	{
		var root:Node = find(node.parent);
		// this line is just to speed up subsequent finds by keeping the tree depth low:
		node.parent = root;
		return root;
	}
}

class Node
{
	public static var pool:Vector.<Node> = new Vector.<Node>();

	public var parent:Node;
	public var treeSize:int;
	
	public function Node() {}
}
