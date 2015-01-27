using UnityEngine;
using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;
using System.Linq;
using Assets.Graph;

public class VoronoiDemo : MonoBehaviour
{
	private int _pointCount = 50;

	private List<Vector2> m_points;
	const float _mapWidth = 50;
	const float _mapHeight = 50;
	private List<LineSegment> _edges = null;
	private List<LineSegment> _spanningTree;
	private List<LineSegment> _delaunayTriangulation;

    Graph _graph;

    const int _textureScale = 5;
    int _textureWidth = (int)_mapWidth * _textureScale;
    int _textureHeight = (int)_mapHeight * _textureScale;

    const int NUM_LLOYD_RELAXATIONS = 2;

	void Awake ()
	{
		Demo ();
	}

	void Update ()
	{
		if (Input.anyKeyDown) {
			Demo ();
		}
	}

	private void Demo ()
	{
				
		List<uint> colors = new List<uint> ();
		m_points = new List<Vector2> ();
			
		for (int i = 0; i < _pointCount; i++) {
			colors.Add (0);
			m_points.Add (new Vector2 (
					UnityEngine.Random.Range (0, _mapWidth),
					UnityEngine.Random.Range (0, _mapHeight))
			);
		}

        for (int i = 0; i < NUM_LLOYD_RELAXATIONS; i++)
        {
            m_points = RelaxPoints(m_points).ToList();
        }

		Delaunay.Voronoi v = new Delaunay.Voronoi (m_points, colors, new Rect (0, 0, _mapWidth, _mapHeight));
		_edges = v.VoronoiDiagram ();
			
		_spanningTree = v.SpanningTree (KruskalType.MINIMUM);
		_delaunayTriangulation = v.DelaunayTriangulation ();

        _graph = new Graph(m_points, v);

        CreateTexture();
	}

    private void CreateTexture()
    {
        Texture2D texture = new Texture2D(_textureWidth, _textureHeight);
        texture.SetPixels(Enumerable.Repeat(Color.magenta, _textureWidth * _textureHeight).ToArray());

        var lines = _edges.Select(p => new[] 
        { 
            ((Vector2)(p.p0)).x, ((Vector2)(p.p0)).y,
            ((Vector2)(p.p1)).x, ((Vector2)(p.p1)).y
        }).ToArray();

        foreach (var line in lines)
        {
            DrawLine(texture, line[0], line[1], line[2], line[3], Color.black);
        }

        //DrawLine(texture, 0, 0, _mapWidth * _textureScale, _mapHeight * _textureScale, Color.yellow);
        texture.FloodFillArea(2 * _textureScale, 2 * _textureScale, Color.red);
        texture.FloodFillArea((int)(_mapWidth / 2 * _textureScale), (int)(_mapHeight / 2 * _textureScale), Color.blue);
        texture.Apply();
        
        var plane = GameObject.Find("Background");
        plane.renderer.material.mainTexture = texture;
        plane.transform.localPosition = new Vector3(_mapWidth / 2, _mapHeight / 2, 1);
    }

    private void DrawLine(Texture2D texture, float x0, float y0, float x1, float y1, Color color)
    {
        texture.DrawLine((int)(x0 * _textureScale), (int)(y0 * _textureScale), (int)(x1 * _textureScale), (int)(y1 * _textureScale), color);
    }

    private IEnumerable<Vector2> RelaxPoints(IEnumerable<Vector2> startingPoints)
    {        
        Delaunay.Voronoi v = new Delaunay.Voronoi(startingPoints.ToList(), null, new Rect(0, 0, _mapWidth, _mapHeight));
        foreach (var point in startingPoints)
        {
            var region = v.Region(point);
            point.Set(0, 0);
            foreach (var r in region)
                point.Set(point.x + r.x, point.y + r.y);

            point.Set(point.x / region.Count, point.y / region.Count);
            yield return point;
        }
    }

	void OnDrawGizmos ()
	{
		Gizmos.color = Color.red;
		if (m_points != null) {
			for (int i = 0; i < m_points.Count; i++) {
				Gizmos.DrawSphere (m_points [i], 0.2f);
			}
		}

        //if (_edges != null) {
        //    Gizmos.color = Color.white;
        //    for (int i = 0; i< _edges.Count; i++) {
        //        Vector2 left = (Vector2)_edges [i].p0;
        //        Vector2 right = (Vector2)_edges [i].p1;
        //        Gizmos.DrawLine ((Vector3)left, (Vector3)right);
        //    }
        //}

		Gizmos.color = Color.yellow;
		Gizmos.DrawLine (new Vector2 (0, 0), new Vector2 (0, _mapHeight));
		Gizmos.DrawLine (new Vector2 (0, 0), new Vector2 (_mapWidth, 0));
		Gizmos.DrawLine (new Vector2 (_mapWidth, 0), new Vector2 (_mapWidth, _mapHeight));
		Gizmos.DrawLine (new Vector2 (0, _mapHeight), new Vector2 (_mapWidth, _mapHeight));
	}
}