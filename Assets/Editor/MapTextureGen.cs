using System;
using System.Collections.Generic;

public static class MapTextureGen
{
    static readonly int[] RockShades = { 0x7A7166, 0x6C645A, 0x5F5850, 0x514B44, 0x3C3732 };

    static readonly int[] BoulderShades = { 0x8E857A, 0x7A7166, 0x675F56, 0x4F4942, 0x35302B };

    static readonly int[] LavaShades = { 0xC4301C, 0xC4301C, 0xC4301C, 0xC4301C };

    static readonly int[] GrassGreens = { 0x8DCB3A, 0x66AD2C, 0x468C26, 0x2F6A22 };
    public const int GrassTuftSize = 8;
    public static byte[] GrassTuft(int variant)
    {
        int w = GrassTuftSize;
        int h = GrassTuftSize;
        int[] pixel = new int[w * h];
        for (int i = 0; i < pixel.Length; i++)
        {
            pixel[i] = -1;
        }
        int[][] columns = { new[] { 2, 4, 5 }, new[] { 1, 3, 5 }, new[] { 2, 3, 6 } };
        int[][] heights = { new[] { 4, 6, 3 }, new[] { 3, 5, 4 }, new[] { 5, 3, 6 } };
        int v = Math.Max(0, Math.Min(2, variant));
        for (int b = 0; b < 3; b++)
        {
            int x = columns[v][b];
            int height = heights[v][b];
            for (int k = 0; k < height; k++)
            {
                pixel[(h - 1 - k) * w + x] = k == height - 1 ? 0 : (k >= height - 3 ? 1 : 2);
            }
        }
        return ToRgba(pixel, w, h, GrassGreens);
    }

    static byte[] ToRgba(int[] pixel, int w, int h, int[] palette)
    {
        byte[] data = new byte[w * h * 4];
        for (int i = 0; i < pixel.Length; i++)
        {
            if (pixel[i] < 0)
            {
                continue;
            }
            int rgb = palette[pixel[i]];
            data[i * 4] = (byte)(rgb >> 16);
            data[i * 4 + 1] = (byte)((rgb >> 8) & 255);
            data[i * 4 + 2] = (byte)(rgb & 255);
            data[i * 4 + 3] = 255;
        }
        return data;
    }

    public static byte[] Rock(int size, int seed)
    {
        Random random = new Random(seed);
        int[] shade = new int[size * size];

        float[] field = Field(size, seed + 3, 4, 8, 0.4f, 0, 0);
        int[] drift = Quantize(field, size, new[] { 0.28f, 0.72f });
        Smooth(drift, size, 3);

        int rows = 5;
        int[] rowHeight = new int[rows];
        int remaining = size;
        for (int r = 0; r < rows; r++)
        {
            int left = rows - r - 1;
            int h = r == rows - 1 ? remaining : size / rows - 3 + random.Next(7);
            h = Math.Max(18, Math.Min(h, remaining - left * 18));
            rowHeight[r] = h;
            remaining -= h;
        }

        int rowStart = 0;
        for (int r = 0; r < rows; r++)
        {
            int h = rowHeight[r];

            int count = 3 + random.Next(2);
            int[] cut = new int[count];
            int offset = random.Next(size);
            int used = 0;
            int[] width = new int[count];
            for (int b = 0; b < count; b++)
            {
                int w = b == count - 1 ? size - used : size / count - 6 + random.Next(13);
                w = Math.Max(22, Math.Min(w, size - used - (count - b - 1) * 22));
                width[b] = w;
                cut[b] = (offset + used) % size;
                used += w;
            }

            int[] baseShade = new int[count];
            for (int b = 0; b < count; b++)
            {
                baseShade[b] = 1 + random.Next(3);
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int block = 0;
                    int distanceLeft = size;
                    for (int b = 0; b < count; b++)
                    {
                        int d = ((x - cut[b]) % size + size) % size;
                        if (d < distanceLeft)
                        {
                            distanceLeft = d;
                            block = b;
                        }
                    }
                    int distanceRight = width[block] - 1 - distanceLeft;

                    int inner = 2;
                    int ix = distanceLeft - inner;
                    int iy = y - inner;
                    int iw = width[block] - inner;
                    int ih = h - inner;

                    int value;
                    bool crevice = ix < 0 || iy < 0;
                    if (!crevice)
                    {
                        int fromRight = iw - 1 - ix;
                        int fromBottom = ih - 1 - iy;
                        crevice = ix + iy < 2 || fromRight + iy < 2 || ix + fromBottom < 2 || fromRight + fromBottom < 2;
                    }
                    if (crevice)
                    {
                        value = 4;
                    }
                    else
                    {
                        value = baseShade[block];
                        int d = drift[Wrap(rowStart + y, size) * size + x];
                        value += d == 0 ? -1 : (d == 2 ? 1 : 0);
                        if (iy < 2)
                        {
                            value -= 1;
                        }
                        else if (iy >= ih - 2)
                        {
                            value += 1;
                        }
                        if (ix == 0 && iy >= 2)
                        {
                            value -= 1;
                        }
                        value = Math.Max(0, Math.Min(3, value));
                    }
                    shade[Wrap(rowStart + y, size) * size + x] = value;
                }
            }
            rowStart += h;
        }

        for (int c = 0; c < 5; c++)
        {
            int x = random.Next(size);
            int y = random.Next(size);
            int length = 7 + random.Next(7);
            int dx = random.Next(3) - 1;
            for (int i = 0; i < length; i++)
            {
                int p = Wrap(y, size) * size + Wrap(x, size);
                if (shade[p] != 4)
                {
                    shade[p] = 3;
                }
                if (random.Next(3) == 0)
                {
                    dx = random.Next(3) - 1;
                }
                x += dx;
                y += 1;
            }
        }
        return ToImage(shade, size, RockShades);
    }

    public static float[] BoulderOutline(int size, int seed)
    {
        Random random = new Random(seed * 7919 + size);
        for (int attempt = 0; attempt < 80; attempt++)
        {
            int count = 8 + random.Next(4);
            double squashX = 1.0 + random.NextDouble() * 0.1;
            double squashY = 0.86 + random.NextDouble() * 0.14;
            double wedge = (random.NextDouble() - 0.5) * 0.4;
            double[] px = new double[count];
            double[] py = new double[count];
            for (int i = 0; i < count; i++)
            {
                double angle = (i + random.NextDouble() * 0.6 - 0.3) / count * Math.PI * 2.0;
                double radius = 0.74 + 0.26 * random.NextDouble();
                double y = Math.Sin(angle) * radius * squashY;
                px[i] = Math.Cos(angle) * radius * squashX + wedge * y;
                py[i] = y;
            }

            int top = 0;
            for (int i = 1; i < count; i++)
            {
                if (py[i] > py[top])
                {
                    top = i;
                }
            }
            int next = (top + 1) % count;
            int previous = (top + count - 1) % count;
            int partner = Math.Abs(py[next] - py[top]) < Math.Abs(py[previous] - py[top]) ? next : previous;
            double flat = Math.Min(py[top], py[partner]);
            py[top] = flat;
            py[partner] = flat;

            List<double[]> hull = ConvexHull(px, py);
            if (hull.Count < 6 || hull.Count > 9)
            {
                continue;
            }

            double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
            for (int i = 0; i < hull.Count; i++)
            {
                minX = Math.Min(minX, hull[i][0]);
                maxX = Math.Max(maxX, hull[i][0]);
                minY = Math.Min(minY, hull[i][1]);
                maxY = Math.Max(maxY, hull[i][1]);
            }
            double scale = (size - 2.0) / Math.Max(maxX - minX, maxY - minY);
            double centreX = (minX + maxX) * 0.5;
            double centreY = (minY + maxY) * 0.5;

            float[] outline = new float[hull.Count * 2];
            for (int i = 0; i < hull.Count; i++)
            {
                outline[i * 2] = (float)(size * 0.5 + (hull[i][0] - centreX) * scale);
                outline[i * 2 + 1] = (float)(size * 0.5 - (hull[i][1] - centreY) * scale);
            }
            return outline;
        }

        float[] octagon = new float[16];
        for (int i = 0; i < 8; i++)
        {
            double angle = (i + 0.5) / 8.0 * Math.PI * 2.0;
            octagon[i * 2] = (float)(size * 0.5 + Math.Cos(angle) * (size * 0.5 - 1.0));
            octagon[i * 2 + 1] = (float)(size * 0.5 - Math.Sin(angle) * (size * 0.5 - 1.0) * 0.85);
        }
        return octagon;
    }

    static List<double[]> ConvexHull(double[] px, double[] py)
    {
        int n = px.Length;
        int[] order = new int[n];
        for (int i = 0; i < n; i++)
        {
            order[i] = i;
        }
        Array.Sort(order, (a, b) => px[a] != px[b] ? px[a].CompareTo(px[b]) : py[a].CompareTo(py[b]));

        List<int> hull = new List<int>();
        for (int pass = 0; pass < 2; pass++)
        {
            int start = hull.Count;
            for (int k = 0; k < n; k++)
            {
                int i = order[pass == 0 ? k : n - 1 - k];
                while (hull.Count >= start + 2)
                {
                    int a = hull[hull.Count - 2];
                    int b = hull[hull.Count - 1];
                    double cross = (px[b] - px[a]) * (py[i] - py[a]) - (py[b] - py[a]) * (px[i] - px[a]);
                    if (cross > 1e-9)
                    {
                        break;
                    }
                    hull.RemoveAt(hull.Count - 1);
                }
                hull.Add(i);
            }
            hull.RemoveAt(hull.Count - 1);
        }

        List<double[]> result = new List<double[]>();
        for (int i = 0; i < hull.Count; i++)
        {
            result.Add(new[] { px[hull[i]], py[hull[i]] });
        }
        return result;
    }

    public static byte[] Boulder(int size, int seed)
    {
        float[] outline = BoulderOutline(size, seed);
        int n = outline.Length / 2;
        Random random = new Random(seed * 131 + 17);

        double cx = 0.0, cy = 0.0;
        for (int i = 0; i < n; i++)
        {
            cx += outline[i * 2];
            cy += outline[i * 2 + 1];
        }
        cx /= n;
        cy /= n;

        double area = 0.0;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            area += outline[i * 2] * outline[j * 2 + 1] - outline[j * 2] * outline[i * 2 + 1];
        }
        double orient = area > 0.0 ? 1.0 : -1.0;

        double[] facetLight = new double[n];
        double[] edgeLight = new double[n];
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            double ex = outline[j * 2] - outline[i * 2];
            double ey = outline[j * 2 + 1] - outline[i * 2 + 1];
            double length = Math.Sqrt(ex * ex + ey * ey);
            double nx = orient * ey / length;
            double ny = -orient * ex / length;
            edgeLight[i] = nx * -0.62 + ny * -0.78;
            facetLight[i] = edgeLight[i] + (random.NextDouble() - 0.5) * 0.45;
        }

        int[] shade = new int[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double px = x + 0.5;
                double py = y + 0.5;

                bool inside = true;
                double nearest = double.MaxValue;
                int nearestEdge = 0;
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    double ax = outline[i * 2], ay = outline[i * 2 + 1];
                    double bx = outline[j * 2], by = outline[j * 2 + 1];
                    double cross = (bx - ax) * (py - ay) - (by - ay) * (px - ax);
                    if (cross * orient < 0.0)
                    {
                        inside = false;
                        break;
                    }
                    double d = DistanceToSegment(px, py, ax, ay, bx, by);
                    if (d < nearest)
                    {
                        nearest = d;
                        nearestEdge = i;
                    }
                }
                if (!inside)
                {
                    shade[y * size + x] = -1;
                    continue;
                }

                int facet = nearestEdge;
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    double c1 = (outline[i * 2] - cx) * (py - cy) - (outline[i * 2 + 1] - cy) * (px - cx);
                    double c2 = (outline[j * 2] - cx) * (py - cy) - (outline[j * 2 + 1] - cy) * (px - cx);
                    if (c1 * orient >= 0.0 && c2 * orient <= 0.0)
                    {
                        facet = i;
                        break;
                    }
                }

                double light = facetLight[facet];
                int value = light > 0.5 ? 0 : (light > 0.12 ? 1 : (light > -0.32 ? 2 : 3));

                if (nearest < 1.0)
                {
                    value = 4;
                }
                else if (nearest < 2.0)
                {
                    if (edgeLight[nearestEdge] > 0.3)
                    {
                        value = Math.Max(0, value - 1);
                    }
                    else if (edgeLight[nearestEdge] < -0.25)
                    {
                        value = Math.Min(3, value + 1);
                    }
                }
                shade[y * size + x] = value;
            }
        }

        int lines = 2 + size / 30;
        for (int c = 0; c < lines; c++)
        {
            int corner = random.Next(n);
            double x = cx;
            double y = cy;
            double tx = outline[corner * 2] - cx;
            double ty = outline[corner * 2 + 1] - cy;
            double norm = Math.Sqrt(tx * tx + ty * ty);
            bool crease = c < 2;
            double direction = Math.Atan2(ty, tx);
            if (!crease)
            {
                x = cx + (random.NextDouble() - 0.5) * size * 0.4;
                y = cy + (random.NextDouble() - 0.5) * size * 0.4;
                direction = random.NextDouble() * Math.PI * 2.0;
            }
            int length = crease ? (int)(norm * 0.55) : size / 6 + random.Next(size / 6);
            for (int i = 0; i < length; i++)
            {
                int ix = (int)Math.Floor(x);
                int iy = (int)Math.Floor(y);
                if (ix >= 0 && iy >= 0 && ix < size && iy < size && shade[iy * size + ix] >= 0 && shade[iy * size + ix] != 4)
                {
                    shade[iy * size + ix] = Math.Min(3, shade[iy * size + ix] + 1);
                    if (!crease && shade[iy * size + ix] < 3)
                    {
                        shade[iy * size + ix] = 3;
                    }
                }
                if (!crease && random.Next(3) == 0)
                {
                    direction += (random.NextDouble() - 0.5) * 1.2;
                }
                x += Math.Cos(direction);
                y += Math.Sin(direction);
            }
        }

        byte[] data = new byte[size * size * 4];
        for (int i = 0; i < shade.Length; i++)
        {
            if (shade[i] < 0)
            {
                continue;
            }
            int rgb = BoulderShades[shade[i]];
            data[i * 4] = (byte)(rgb >> 16);
            data[i * 4 + 1] = (byte)((rgb >> 8) & 255);
            data[i * 4 + 2] = (byte)(rgb & 255);
            data[i * 4 + 3] = 255;
        }
        return data;
    }

    static double DistanceToSegment(double px, double py, double ax, double ay, double bx, double by)
    {
        double abx = bx - ax;
        double aby = by - ay;
        double lengthSquared = abx * abx + aby * aby;
        double t = lengthSquared > 0.0 ? Math.Max(0.0, Math.Min(1.0, ((px - ax) * abx + (py - ay) * aby) / lengthSquared)) : 0.0;
        double dx = px - (ax + abx * t);
        double dy = py - (ay + aby * t);
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static byte[] Lava(int size, int seed, int frame)
    {
        float[] field = Field(size, seed, 2, 4, 0.3f, frame * size / 4, frame * size / 4);
        int[] shade = Quantize(field, size, new[] { 0.28f, 0.58f, 0.88f });
        Smooth(shade, size, 2);
        return ToImage(shade, size, LavaShades);
    }

    static float[] Field(int size, int seed, int cellsA, int cellsB, float weightB, int shiftX, int shiftY)
    {
        float[] result = new float[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float a = Noise(x + shiftX, y + shiftY, size, cellsA, seed);
                float b = Noise(x + shiftX, y + shiftY, size, cellsB, seed + 101);
                result[y * size + x] = (a + b * weightB) / (1f + weightB);
            }
        }
        return result;
    }

    static float Noise(int x, int y, int size, int cells, int seed)
    {
        float cell = (float)size / cells;
        float fx = Wrap(x, size) / cell;
        float fy = Wrap(y, size) / cell;
        int x0 = (int)Math.Floor(fx);
        int y0 = (int)Math.Floor(fy);
        float tx = Smooth01(fx - x0);
        float ty = Smooth01(fy - y0);
        float top = Lerp(Lattice(x0, y0, cells, seed), Lattice(x0 + 1, y0, cells, seed), tx);
        float bottom = Lerp(Lattice(x0, y0 + 1, cells, seed), Lattice(x0 + 1, y0 + 1, cells, seed), tx);
        return Lerp(top, bottom, ty);
    }

    static float Lattice(int x, int y, int cells, int seed)
    {
        x = Wrap(x, cells);
        y = Wrap(y, cells);
        unchecked
        {
            uint h = (uint)(x * 73856093 ^ y * 19349663 ^ seed * 83492791);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return (h & 0xffff) / 65535f;
        }
    }

    static float Smooth01(float t)
    {
        return t * t * (3f - 2f * t);
    }

    static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    static int Wrap(int v, int size)
    {
        return ((v % size) + size) % size;
    }

    static int[] Quantize(float[] field, int size, float[] cuts)
    {
        float[] sorted = (float[])field.Clone();
        Array.Sort(sorted);
        float[] limits = new float[cuts.Length];
        for (int c = 0; c < cuts.Length; c++)
        {
            limits[c] = sorted[Math.Min(sorted.Length - 1, (int)(cuts[c] * sorted.Length))];
        }
        int[] shade = new int[field.Length];
        for (int i = 0; i < field.Length; i++)
        {
            int s = 0;
            while (s < limits.Length && field[i] >= limits[s])
            {
                s++;
            }
            shade[i] = s;
        }
        return shade;
    }

    static void Smooth(int[] shade, int size, int passes)
    {
        int[] copy = new int[shade.Length];
        int[] votes = new int[8];
        for (int pass = 0; pass < passes; pass++)
        {
            Array.Copy(shade, copy, shade.Length);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Array.Clear(votes, 0, votes.Length);
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            votes[copy[Wrap(y + oy, size) * size + Wrap(x + ox, size)]]++;
                        }
                    }
                    int own = copy[y * size + x];
                    int best = own;
                    for (int s = 0; s < votes.Length; s++)
                    {
                        if (votes[s] > votes[best])
                        {
                            best = s;
                        }
                    }
                    shade[y * size + x] = best;
                }
            }
        }
    }

    static byte[] ToImage(int[] shade, int size, int[] palette)
    {
        byte[] data = new byte[size * size * 4];
        for (int i = 0; i < shade.Length; i++)
        {
            int rgb = palette[shade[i]];
            data[i * 4] = (byte)(rgb >> 16);
            data[i * 4 + 1] = (byte)((rgb >> 8) & 255);
            data[i * 4 + 2] = (byte)(rgb & 255);
            data[i * 4 + 3] = 255;
        }
        return data;
    }
    static double Rand01(int a, int b, int c)
    {
        unchecked
        {
            uint h = (uint)(a * 73856093 ^ b * 19349663 ^ c * 83492791);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return (h & 0xffff) / 65535.0;
        }
    }
    static void SetPixel(byte[] data, int width, int x, int y, int rgb, int alpha = 255)
    {
        int i = (y * width + x) * 4;
        data[i] = (byte)(rgb >> 16);
        data[i + 1] = (byte)((rgb >> 8) & 255);
        data[i + 2] = (byte)(rgb & 255);
        data[i + 3] = (byte)alpha;
    }

    public const int MoonSize = 600;
    public static byte[] CheeseMoon()
    {
        int size = MoonSize;
        double half = size * 0.5;
        double radius = half - 2.0;
        int[] body = { 0xFFF1A8, 0xF6E27A, 0xE8C95A, 0xD2A93F };
        int[] pixel = new int[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double dx = x + 0.5 - half;
                double dy = y + 0.5 - half;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d > radius)
                {
                    pixel[y * size + x] = -1;
                    continue;
                }
                double lit = -0.55 * dx / radius - 0.75 * dy / radius + 0.12 * (Rand01(x >> 3, y >> 3, 5) - 0.5);
                int c = lit > 0.38 ? 0 : (lit > 0.0 ? 1 : (lit > -0.4 ? 2 : 3));
                if (d > radius - 3.0)
                {
                    c = 4;
                }
                pixel[y * size + x] = c;
            }
        }
        int[] palette = { body[0], body[1], body[2], body[3], 0xB98E2F };
        byte[] data = new byte[size * size * 4];
        for (int i = 0; i < pixel.Length; i++)
        {
            if (pixel[i] >= 0)
            {
                SetPixel(data, size, i % size, i / size, palette[pixel[i]]);
            }
        }

        for (int k = 0; k < 22; k++)
        {
            bool hole = k >= 6;
            double angle = Rand01(k, 1, 1) * Math.PI * 2.0;
            double distance = Math.Sqrt(Rand01(k, 2, 1)) * radius * 0.82;
            double cx = half + Math.Cos(angle) * distance;
            double cy = half + Math.Sin(angle) * distance;
            double cr = hole ? radius * (0.035 + 0.075 * Rand01(k, 3, 1)) : radius * (0.12 + 0.07 * Rand01(k, 3, 1));
            for (int y = (int)(cy - cr - 2); y <= (int)(cy + cr + 2); y++)
            {
                for (int x = (int)(cx - cr - 2); x <= (int)(cx + cr + 2); x++)
                {
                    if (x < 0 || y < 0 || x >= size || y >= size || pixel[y * size + x] < 0 || pixel[y * size + x] == 4)
                    {
                        continue;
                    }
                    double ddx = x + 0.5 - cx;
                    double ddy = y + 0.5 - cy;
                    double dd = Math.Sqrt(ddx * ddx + ddy * ddy);
                    if (dd > cr)
                    {
                        continue;
                    }
                    int rgb;
                    if (hole)
                    {
                        rgb = dd > cr - 2.0 && ddx + ddy > 0 ? 0xFFF6C4 : (ddx + ddy < -cr * 0.4 ? 0x7C581A : 0xA37424);
                    }
                    else
                    {
                        rgb = dd > cr - 2.5 ? (ddx + ddy > 0 ? 0xF6E27A : 0xB98E2F) : 0xD9AE45;
                    }
                    SetPixel(data, size, x, y, rgb);
                }
            }
        }
        return data;
    }
}
