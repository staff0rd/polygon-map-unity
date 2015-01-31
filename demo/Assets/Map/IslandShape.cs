using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Map
{
    public class IslandShape
    {
        // This class has factory functions for generating islands of
        // different shapes. The factory returns a function that takes a
        // normalized point (x and y are -1 to +1) and returns true if the
        // point should be on the island, and false if it should be water
        // (lake or ocean).

        // The radial island radius is based on overlapping sine waves 
        public static float ISLAND_FACTOR = 1.07f;  // 1.0 means no small islands; 2.0 leads to a lot
        public static System.Func<Vector2, bool> makeRadial()
        {
            var bumps = Random.Range(1, 6);
            var startAngle = Random.value * 2 * Mathf.PI;
            var dipAngle = Random.value * 2 * Mathf.PI;

            var random = Random.value;
            var start = 0.2f;
            var end = 0.7f;

            var dipWidth = (end - start) * random + start;

            System.Func<Vector2, bool> inside = q =>
            {
                var angle = Mathf.Atan2(q.y, q.x);
                var length = 0.5 * (Mathf.Max(Mathf.Abs(q.x), Mathf.Abs(q.y)) + q.magnitude);

                var r1 = 0.5 + 0.40 * Mathf.Sin(startAngle + bumps * angle + Mathf.Cos((bumps + 3) * angle));
                var r2 = 0.7 - 0.20 * Mathf.Sin(startAngle + bumps * angle - Mathf.Sin((bumps + 2) * angle));
                if (Mathf.Abs(angle - dipAngle) < dipWidth
                    || Mathf.Abs(angle - dipAngle + 2 * Mathf.PI) < dipWidth
                    || Mathf.Abs(angle - dipAngle - 2 * Mathf.PI) < dipWidth)
                {
                    r1 = r2 = 0.2;
                }
                var result = (length < r1 || (length > r1 * ISLAND_FACTOR && length < r2));
                return result;
            };

            return inside;
        }

        // The Perlin-based island combines perlin noise with the radius
        public static System.Func<Vector2, bool> makePerlin()
        {
            var offset = Random.Range(0, 100000);
            System.Func<Vector2, bool> inside = q =>
            {
                var x = q.x + offset;
                var y = q.y + offset;
                var perlin = Mathf.PerlinNoise(x/10 , y/10);
                var checkValue = (0.3 + 0.3 * q.magnitude * q.magnitude);
                var result = perlin > .3;
                return result;
            };
            return inside;
        }

        // The square shape fills the entire space with land
        public static System.Func<Vector2, bool> makeSquare()
        {
            System.Func<Vector2, bool> inside = q => { return true; };
            return inside;
        }
    }
}
