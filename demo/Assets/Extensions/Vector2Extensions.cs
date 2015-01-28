using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class Vector2Extensions
{
    public static Vector2 Interpolate(Vector2 pt1, Vector2 pt2, float f)
    {
        var x = f * pt1.x + (1 - f) * pt2.x;
        var y = f * pt1.y + (1 - f) * pt2.y;

        return new Vector2(x, y);
    }

    public static void DrawLine(this Texture2D tex, int x0, int y0, int x1, int y1, Color col)
    {
        int dy = (int)(y1 - y0);
        int dx = (int)(x1 - x0);
        int stepx, stepy;

        if (dy < 0) { dy = -dy; stepy = -1; }
        else { stepy = 1; }
        if (dx < 0) { dx = -dx; stepx = -1; }
        else { stepx = 1; }
        dy <<= 1;
        dx <<= 1;

        float fraction = 0;

        tex.SetPixel(x0, y0, col);
        if (dx > dy)
        {
            fraction = dy - (dx >> 1);
            while (Mathf.Abs(x0 - x1) > 1)
            {
                if (fraction >= 0)
                {
                    y0 += stepy;
                    fraction -= dx;
                }
                x0 += stepx;
                fraction += dy;
                tex.SetPixel(x0, y0, col);
            }
        }
        else
        {
            fraction = dx - (dy >> 1);
            while (Mathf.Abs(y0 - y1) > 1)
            {
                if (fraction >= 0)
                {
                    x0 += stepx;
                    fraction -= dy;
                }
                y0 += stepy;
                fraction += dx;
                tex.SetPixel(x0, y0, col);
            }
        }
    }

    public struct Point
    {
        public short x;
        public short y;
        public Point(short aX, short aY) { x = aX; y = aY; }
        public Point(int aX, int aY) : this((short)aX, (short)aY) { }
    }

    public static void FloodFillArea(this Texture2D aTex, int aX, int aY, Color aFillColor)
    {
        int w = aTex.width;
        int h = aTex.height;
        Color[] colors = aTex.GetPixels();
        Color refCol = colors[aX + aY * w];
        Queue<Point> nodes = new Queue<Point>();
        nodes.Enqueue(new Point(aX, aY));
        while (nodes.Count > 0)
        {
            Point current = nodes.Dequeue();
            for (int i = current.x; i < w; i++)
            {
                Color C = colors[i + current.y * w];
                if (C != refCol || C == aFillColor)
                    break;
                colors[i + current.y * w] = aFillColor;
                if (current.y + 1 < h)
                {
                    C = colors[i + current.y * w + w];
                    if (C == refCol && C != aFillColor)
                        nodes.Enqueue(new Point(i, current.y + 1));
                }
                if (current.y - 1 >= 0)
                {
                    C = colors[i + current.y * w - w];
                    if (C == refCol && C != aFillColor)
                        nodes.Enqueue(new Point(i, current.y - 1));
                }
            }
            for (int i = current.x - 1; i >= 0; i--)
            {
                Color C = colors[i + current.y * w];
                if (C != refCol || C == aFillColor)
                    break;
                colors[i + current.y * w] = aFillColor;
                if (current.y + 1 < h)
                {
                    C = colors[i + current.y * w + w];
                    if (C == refCol && C != aFillColor)
                        nodes.Enqueue(new Point(i, current.y + 1));
                }
                if (current.y - 1 >= 0)
                {
                    C = colors[i + current.y * w - w];
                    if (C == refCol && C != aFillColor)
                        nodes.Enqueue(new Point(i, current.y - 1));
                }
            }
        }
        aTex.SetPixels(colors);
    }

    public static void FloodFillBorder(this Texture2D aTex, int aX, int aY, Color aFillColor, Color aBorderColor)
    {
        int w = aTex.width;
        int h = aTex.height;
        Color[] colors = aTex.GetPixels();
        byte[] checkedPixels = new byte[colors.Length];
        Color refCol = aBorderColor;
        Queue<Point> nodes = new Queue<Point>();
        nodes.Enqueue(new Point(aX, aY));
        while (nodes.Count > 0)
        {
            Point current = nodes.Dequeue();

            for (int i = current.x; i < w; i++)
            {
                if (checkedPixels[i + current.y * w] > 0 || colors[i + current.y * w] == refCol)
                    break;
                colors[i + current.y * w] = aFillColor;
                checkedPixels[i + current.y * w] = 1;
                if (current.y + 1 < h)
                {
                    if (checkedPixels[i + current.y * w + w] == 0 && colors[i + current.y * w + w] != refCol)
                        nodes.Enqueue(new Point(i, current.y + 1));
                }
                if (current.y - 1 >= 0)
                {
                    if (checkedPixels[i + current.y * w - w] == 0 && colors[i + current.y * w - w] != refCol)
                        nodes.Enqueue(new Point(i, current.y - 1));
                }
            }
            for (int i = current.x - 1; i >= 0; i--)
            {
                if (checkedPixels[i + current.y * w] > 0 || colors[i + current.y * w] == refCol)
                    break;
                colors[i + current.y * w] = aFillColor;
                checkedPixels[i + current.y * w] = 1;
                if (current.y + 1 < h)
                {
                    if (checkedPixels[i + current.y * w + w] == 0 && colors[i + current.y * w + w] != refCol)
                        nodes.Enqueue(new Point(i, current.y + 1));
                }
                if (current.y - 1 >= 0)
                {
                    if (checkedPixels[i + current.y * w - w] == 0 && colors[i + current.y * w - w] != refCol)
                        nodes.Enqueue(new Point(i, current.y - 1));
                }
            }
        }
        aTex.SetPixels(colors);
    }

    public static void FillPolygon(this Texture2D texture, Vector2[] points, Color color)
    {
        // http://alienryderflex.com/polygon_fill/
        var IMAGE_BOT = (int)points.Max(p => p.y);
        var IMAGE_TOP = (int)points.Min(p => p.y);
        var IMAGE_LEFT = (int)points.Min(p => p.x);
        var IMAGE_RIGHT = (int)points.Max(p => p.x);
        var MAX_POLY_CORNERS = points.Count();
        var polyCorners = MAX_POLY_CORNERS;
        var polyY = points.Select(p => p.y).ToArray();
        var polyX = points.Select(p => p.x).ToArray();
        int[] nodeX = new int[MAX_POLY_CORNERS];
        int nodes, pixelX, i, j, swap;

        //  Loop through the rows of the image.
        for (int pixelY = IMAGE_TOP; pixelY < IMAGE_BOT; pixelY++)
        {

            //  Build a list of nodes.
            nodes = 0;
            j = polyCorners - 1;
            for (i = 0; i < polyCorners; i++)
            {
                if (polyY[i] < (float)pixelY && polyY[j] >= (float)pixelY || polyY[j] < (float)pixelY && polyY[i] >= (float)pixelY)
                {
                    nodeX[nodes++] = (int)(polyX[i] + (pixelY - polyY[i]) / (polyY[j] - polyY[i]) * (polyX[j] - polyX[i]));
                }
                j = i;
            }

            //  Sort the nodes, via a simple “Bubble” sort.
            i = 0;
            while (i < nodes - 1)
            {
                if (nodeX[i] > nodeX[i + 1])
                {
                    swap = nodeX[i]; nodeX[i] = nodeX[i + 1]; nodeX[i + 1] = swap; if (i != 0) i--;
                }
                else
                {
                    i++;
                }
            }

            //  Fill the pixels between node pairs.
            for (i = 0; i < nodes; i += 2)
            {
                if (nodeX[i] >= IMAGE_RIGHT) break;
                if (nodeX[i + 1] > IMAGE_LEFT)
                {
                    if (nodeX[i] < IMAGE_LEFT) 
                        nodeX[i] = IMAGE_LEFT;
                    if (nodeX[i + 1] > IMAGE_RIGHT) 
                        nodeX[i + 1] = IMAGE_RIGHT;
                    for (j = nodeX[i]; j < nodeX[i + 1]; j++)
                        texture.SetPixel(j, pixelY, color);
                }
            }
        }
    }
}


