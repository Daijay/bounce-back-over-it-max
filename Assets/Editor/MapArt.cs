using System;

public static class MapArt
{
    static readonly int[] Greens = { 0x8DCB3A, 0x66AD2C, 0x468C26, 0x2F6A22 };
    static readonly int[] Dirt = { 0x7A5634, 0x654529, 0x52391F, 0x3E2B17 };
    static readonly int[] Pebble = { 0x9A9082, 0x7D7466, 0x5E574C };


    static uint H(int a, int b, int c)
    {
        unchecked
        {
            uint h = (uint)(a * 73856093 ^ b * 19349663 ^ c * 83492791);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return h;
        }
    }

    static int R100(int a, int b, int c)
    {
        return (int)(H(a, b, c) % 100u);
    }

    static double Noise1(double x, double cell, int seed)
    {
        double f = x / cell;
        int i = (int)Math.Floor(f);
        double t = f - i;
        t = t * t * (3.0 - 2.0 * t);
        double a = (H(i, 0, seed) & 0xffffu) / 65535.0;
        double b = (H(i + 1, 0, seed) & 0xffffu) / 65535.0;
        return a + (b - a) * t;
    }

    static int Mod(int v, int m)
    {
        return ((v % m) + m) % m;
    }

    static void Set(byte[] data, int w, int x, int y, int rgb, int alpha = 255)
    {
        int i = (y * w + x) * 4;
        data[i] = (byte)(rgb >> 16);
        data[i + 1] = (byte)((rgb >> 8) & 255);
        data[i + 2] = (byte)(rgb & 255);
        data[i + 3] = (byte)alpha;
    }


    public const float FootStart = 12f;
    public const float FootEnd = 31f;
    public const float FootTop = 7.2f;
    public const int FootSegments = 12;
    public const float GrassTaperStart = 17f;
    public const float DirtTaperStart = 15.5f;

    public static float FootX(int i)
    {
        return FootStart + (FootEnd - FootStart) * i / FootSegments;
    }

    public static float FootY(int i)
    {
        float t = (float)i / FootSegments;
        return FootTop * t * t;
    }

    public static float FootHeight(float x)
    {
        if (x <= FootStart)
        {
            return 0f;
        }
        if (x >= FootEnd)
        {
            return FootTop;
        }
        float f = (x - FootStart) / (FootEnd - FootStart) * FootSegments;
        int i = (int)Math.Floor(f);
        float t = f - i;
        return FootY(i) + (FootY(i + 1) - FootY(i)) * t;
    }

    public static byte[] Ground(int w, int h, int lineRow, int originPixelX, byte[] rockTile, int seed, int[] rise, float[] grassScale, float[] dirtScale)
    {
        byte[] data = new byte[w * h * 4];

        int[] surface = new int[w];
        int[] grassDepth = new int[w];
        int[] stoneStart = new int[w];
        int[] blade = new int[w];
        for (int c = 0; c < w; c++)
        {
            surface[c] = lineRow - rise[c];

            double g = 0.6 * Noise1(c, 36, seed + 1) + 0.4 * Noise1(c, 11, seed + 2);
            grassDepth[c] = grassScale[c] < 0.02f ? 0 : Math.Max(2, (int)Math.Round((17 + 16.0 * g) * grassScale[c]));

            double d = Noise1(c, 80, seed + 3);
            int jag = (int)Math.Round((Noise1(c, 7, seed + 4) - 0.5) * 10.0);
            stoneStart[c] = grassDepth[c] + (int)Math.Round((106 + 54.0 * d + jag) * dirtScale[c]);

            bool clump = Noise1(c, 9, seed + 5) > 0.68;
            bool previous = c > 0 && blade[c - 1] > 0;
            if (grassScale[c] > 0.4f && ((!previous && R100(c, 1, seed) < 36) || (clump && R100(c, 2, seed) < 70)))
            {
                blade[c] = 1 + (int)(H(c, 3, seed) % 4u);
            }
        }

        for (int y = 0; y < h; y++)
        {
            int worldRow = lineRow - y - 1;
            for (int x = 0; x < w; x++)
            {
                int depth = y - surface[x];
                if (depth < 0)
                {
                    int height = -depth;
                    if (blade[x] >= height)
                    {
                        Set(data, w, x, y, blade[x] == height ? Greens[0] : Greens[1]);
                    }
                    continue;
                }

                int rgb;
                if (depth < grassDepth[x])
                {
                    rgb = GrassPixel(x, depth, grassDepth[x], seed);
                }
                else if (depth < stoneStart[x])
                {
                    rgb = DirtPixel(x, depth - grassDepth[x], stoneStart[x] - grassDepth[x], seed);
                }
                else
                {
                    int u = Mod(originPixelX + x, 128);
                    int v = 127 - Mod(worldRow, 128);
                    int i = (v * 128 + u) * 4;
                    rgb = (rockTile[i] << 16) | (rockTile[i + 1] << 8) | rockTile[i + 2];
                    int into = depth - stoneStart[x];
                    if (into < 3 && dirtScale[x] > 0.3f)
                    {
                        rgb = MixRgb(rgb, Dirt[3], into == 0 ? 0.7 : (into == 1 ? 0.45 : 0.2));
                    }
                }
                Set(data, w, x, y, rgb);
            }
        }

        for (int cy = 0; cy * 12 < h; cy++)
        {
            for (int cx = 0; cx * 12 < w; cx++)
            {
                if (R100(cx, cy, seed + 9) >= 30)
                {
                    continue;
                }
                int px = cx * 12 + (int)(H(cx, cy, seed + 10) % 9u);
                int py = cy * 12 + (int)(H(cx, cy, seed + 11) % 9u);
                int pw = 2 + (int)(H(cx, cy, seed + 12) % 2u);
                int ph = 2;
                if (px + pw >= w || py + ph >= h)
                {
                    continue;
                }
                bool inDirt = true;
                for (int x = px; x < px + pw && inDirt; x++)
                {
                    int depthTop = py - surface[x];
                    int depthBottom = py + ph - 1 - surface[x];
                    inDirt = depthTop >= grassDepth[x] + 3 && depthBottom < stoneStart[x] - 3;
                }
                if (!inDirt)
                {
                    continue;
                }
                for (int y = 0; y < ph; y++)
                {
                    for (int x = 0; x < pw; x++)
                    {
                        int colour = (x == 0 && y == 0) ? Pebble[0] : (y == ph - 1 || x == pw - 1 ? Pebble[2] : Pebble[1]);
                        Set(data, w, px + x, py + y, colour);
                    }
                }
            }
        }
        return data;
    }

    static int GrassPixel(int x, int depth, int grassDepth, int seed)
    {
        if (depth < 3)
        {
            return R100(x, depth, seed + 20) < 14 ? Greens[1] : Greens[0];
        }
        int fromBottom = grassDepth - 1 - depth;
        if (fromBottom < 2)
        {
            return fromBottom == 0 ? Greens[3] : (R100(x, depth, seed + 21) < 50 ? Greens[3] : Greens[2]);
        }
        if (depth < grassDepth * 0.5)
        {
            return R100(x, depth, seed + 22) < 22 ? Greens[0] : Greens[1];
        }
        return R100(x, depth, seed + 23) < 22 ? Greens[3] : Greens[2];
    }

    static int DirtPixel(int x, int depth, int thickness, int seed)
    {
        double t = (double)depth / Math.Max(1, thickness);
        int shade = t < 0.25 ? 0 : (t < 0.55 ? 1 : (t < 0.8 ? 2 : 3));
        int blot = R100(x >> 1, depth >> 1, seed + 30);
        if (blot < 16 && shade > 0)
        {
            shade--;
        }
        else if (blot > 86 && shade < 3)
        {
            shade++;
        }
        return Dirt[shade];
    }

    static int MixRgb(int a, int b, double t)
    {
        int r = (int)Math.Round(((a >> 16) & 255) * (1 - t) + ((b >> 16) & 255) * t);
        int g = (int)Math.Round(((a >> 8) & 255) * (1 - t) + ((b >> 8) & 255) * t);
        int bl = (int)Math.Round((a & 255) * (1 - t) + (b & 255) * t);
        return (r << 16) | (g << 8) | bl;
    }


    static readonly int[] PadGreens = { 0xA8F060, 0x7FD14A, 0x3FA23A, 0x2F8A33, 0x1D5E2A, 0x14421E };

    public static byte[] PadLook(int w)
    {
        int h = 13;
        byte[] data = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int fromSide = Math.Min(x, w - 1 - x);
                if ((y == 0 && fromSide < 2) || (y == h - 1 && fromSide < 2) || (y == 1 && fromSide < 1) || (y == h - 2 && fromSide < 1))
                {
                    continue;
                }
                int colour;
                if (fromSide == 0 || y == h - 1)
                {
                    colour = PadGreens[5];
                }
                else if (y == 0)
                {
                    colour = PadGreens[0];
                }
                else if (y <= 2)
                {
                    colour = PadGreens[1];
                }
                else if (y >= h - 3 || fromSide == 1)
                {
                    colour = PadGreens[4];
                }
                else
                {
                    colour = ((x / 6) & 1) == 0 ? PadGreens[2] : PadGreens[3];
                }
                Set(data, w, x, y, colour);
            }
        }
        return data;
    }

    public static byte[] PadArrow()
    {
        int w = 15;
        int h = 7;
        byte[] data = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool left = x == 6 - y || x == 7 - y;
                bool right = x == 7 + y || x == 8 + y;
                if (left || right)
                {
                    Set(data, w, x, y, (x == 6 - y || x == 8 + y) ? 0x7CC040 : 0xD2FA8C);
                }
            }
        }
        return data;
    }


    public static byte[] Snow(int seed)
    {
        int size = 64;
        int[] palette = { 0xF4F8FF, 0xDCE8F6, 0xC2D6EC, 0xFFFFFF };
        byte[] data = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double a = Lattice(x / 8, y / 8, 8, seed);
                double b = Lattice(x / 4, y / 4, 16, seed + 5);
                double v = 0.65 * a + 0.35 * b;
                int shade = 0;
                Set(data, size, x, y, palette[shade]);
            }
        }
        return data;
    }

    static double Lattice(int x, int y, int cells, int seed)
    {
        return (H(Mod(x, cells), Mod(y, cells), seed) & 0xffffu) / 65535.0;
    }


    public static byte[] ForceField()
    {
        int w = 16;
        int h = 32;
        byte[] data = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int column = x % 8;
                int alpha = 30;
                int rgb = 0xFF2A2A;
                if (column == 3 || column == 4)
                {
                    alpha = 215;
                    rgb = 0xFF5A5A;
                }
                else if (column == 2 || column == 5)
                {
                    alpha = 110;
                }
                if ((y % 16) < 2)
                {
                    alpha = Math.Min(255, alpha + 80);
                }
                Set(data, w, x, y, rgb, alpha);
            }
        }
        return data;
    }
    public static byte[] White(int w, int h)
    {
        byte[] data = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Set(data, w, x, y, 0xFFFFFF);
            }
        }
        return data;
    }
    public static byte[] Ring()
    {
        int w = 128;
        byte[] data = new byte[w * w * 4];
        for (int y = 0; y < w; y++)
        {
            for (int x = 0; x < w; x++)
            {
                double dx = (x - 63.5) / 64.0;
                double dy = (y - 63.5) / 64.0;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d > 1.0 || d < 0.78)
                {
                    continue;
                }
                Set(data, w, x, y, 0xFFFFFF, d > 0.9 ? 255 : (d > 0.84 ? 150 : 70));
            }
        }
        return data;
    }
}
