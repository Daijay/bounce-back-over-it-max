using System;

public static class BackgroundTreeMaker
{
    static readonly int[] Browns = { 0x251A12, 0x32241A, 0x41301F, 0x523B28, 0x634A33, 0x765B40 };
    static readonly int[] Greens = { 0x1B2A15, 0x25391B, 0x30491F, 0x3E5C27 };
    const int Mist = 0x5A5C4C;

    public static BackgroundLayerSplitter.LayerImage Make(string name, BackgroundLayerSplitter.TreeSettings settings,
        int layerSetting, int sourceWidth, int height, int grassTopRow, float heightScale)
    {
        int period = Math.Max(settings.period + settings.periodStep * layerSetting, sourceWidth * 2);
        period -= period % 2;
        byte[] data = new byte[period * height * 4];
        Random random = new Random(settings.seed + 101 * layerSetting);

        int[] brown = Tint(Browns, settings.darkness[layerSetting], settings.haze[layerSetting]);
        int[] green = Tint(Greens, settings.darkness[layerSetting], settings.haze[layerSetting]);

        int minHeight = Math.Max(6, (int)Math.Round(settings.minHeight[layerSetting] * heightScale));
        int maxHeight = Math.Max(minHeight, (int)Math.Round(settings.maxHeight[layerSetting] * heightScale));
        int groundRow = grassTopRow + Math.Max(0, settings.sink);
        double spacing = Math.Max(4, settings.spacing[layerSetting]);

        double x = random.NextDouble() * spacing;
        int id = 0;
        while (x < period)
        {
            double roll = random.NextDouble();
            double advance;
            if (roll < 0.35)
            {
                advance = spacing * (0.3 + random.NextDouble() * 0.4);
            }
            else if (roll < 0.9)
            {
                advance = spacing * (0.8 + random.NextDouble() * 0.5);
            }
            else
            {
                advance = spacing * (1.8 + random.NextDouble() * 1.2);
            }

            int treeHeight = minHeight + (int)Math.Round(random.NextDouble() * (maxHeight - minHeight));
            DrawTree(data, period, height, (int)Math.Round(x), groundRow, treeHeight, brown, green, layerSetting * 100003 + id, random);
            id++;
            x += Math.Max(2.0, advance);
        }

        int pad = (period - sourceWidth) / 2;
        return new BackgroundLayerSplitter.LayerImage
        {
            name = name, width = period, height = height, pad = pad, mirrored = false, rgba = data
        };
    }

    static int[] Tint(int[] source, float darkness, float haze)
    {
        int[] result = new int[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            int r = Mix(((source[i] >> 16) & 255) * darkness, (Mist >> 16) & 255, haze);
            int g = Mix(((source[i] >> 8) & 255) * darkness, (Mist >> 8) & 255, haze);
            int b = Mix((source[i] & 255) * darkness, Mist & 255, haze);
            result[i] = (r << 16) | (g << 8) | b;
        }
        return result;
    }

    static int Mix(double a, double b, double t)
    {
        return Math.Max(0, Math.Min(255, (int)Math.Round(a + (b - a) * t)));
    }

    static void DrawTree(byte[] data, int period, int height, int centre, int groundRow, int treeHeight,
        int[] brown, int[] green, int id, Random random)
    {
        int top = groundRow - treeHeight + 1;

        double trunkFraction = 0.5 + random.NextDouble() * 0.2;
        int canopyRows = Math.Max(3, treeHeight - (int)Math.Round(treeHeight * trunkFraction));
        int canopyBottom = top + canopyRows - 1;

        int trunkWidth = Math.Max(3, (int)Math.Round(treeHeight * (0.13 + 0.06 * random.NextDouble())));
        int trunkLeft = centre - trunkWidth / 2;
        int trunkTop = Math.Max(top, canopyBottom - 1);
        int baseShade = random.Next(5);
        for (int y = trunkTop; y < height; y++)
        {
            for (int t = 0; t < trunkWidth; t++)
            {
                int shade = baseShade;
                if (t == 0)
                {
                    shade += 1;
                }
                else if (t == trunkWidth - 1)
                {
                    shade -= 1;
                }
                else
                {
                    int n = Hash(id * 7 + t, y >> 2, 5) % 100;
                    if (n < 14)
                    {
                        shade -= 1;
                    }
                    else if (n > 90)
                    {
                        shade += 1;
                    }
                }
                Put(data, period, height, trunkLeft + t, y, brown[Math.Max(0, Math.Min(brown.Length - 1, shade))]);
            }
        }

        int visibleEnd = groundRow - 4;
        int span = visibleEnd - trunkTop - 3;
        if (span > 6)
        {
            int cracks = 1 + random.Next(2) + (trunkWidth >= 6 ? 1 : 0);
            for (int c = 0; c < cracks; c++)
            {
                int cx = trunkLeft + 1 + random.Next(Math.Max(1, trunkWidth - 2));
                int cy = trunkTop + 2 + random.Next(span);
                int length = 3 + random.Next(5);
                for (int i = 0; i < length && cy < visibleEnd; i++)
                {
                    Put(data, period, height, cx, cy, brown[0]);
                    if (random.Next(3) == 0)
                    {
                        cx = Math.Max(trunkLeft + 1, Math.Min(trunkLeft + trunkWidth - 2, cx + random.Next(3) - 1));
                    }
                    cy++;
                }
            }
            if (trunkWidth >= 5)
            {
                int knots = random.Next(3);
                for (int k = 0; k < knots; k++)
                {
                    int kx = trunkLeft + 1 + random.Next(Math.Max(1, trunkWidth - 4));
                    int ky = trunkTop + 2 + random.Next(Math.Max(1, span - 3));
                    int light = Math.Min(brown.Length - 1, baseShade + 2);
                    Put(data, period, height, kx, ky, brown[light]);
                    Put(data, period, height, kx + 1, ky, brown[0]);
                    Put(data, period, height, kx, ky + 1, brown[0]);
                    Put(data, period, height, kx + 1, ky + 1, brown[1]);
                }
            }
        }

        double fill = 0.6 + random.NextDouble() * 0.4;
        double halfW = Math.Max(4.0, treeHeight * (0.15 + 0.08 * random.NextDouble()));
        halfW = Math.Max(halfW, trunkWidth * 0.5 + 2);
        double halfH = canopyRows / 2.0;
        double centreY = top + halfH - 0.5;
        int reach = (int)Math.Ceiling(halfW);

        for (int r = 0; r < canopyRows; r++)
        {
            double dy = top + r - centreY;
            for (int dx = -reach; dx <= reach; dx++)
            {
                double nx = dx / halfW;
                double ny = dy / halfH;
                double d = nx * nx + ny * ny;
                if (d > 1.0)
                {
                    continue;
                }

                double noise = (Hash(id * 31 + (dx >> 1), r >> 1, 3) % 1000) / 1000.0;
                double threshold = fill + 0.25 * (0.5 - d);
                bool leaf = fill >= 0.999 || d < 0.12 || noise < threshold;
                if (!leaf)
                {
                    continue;
                }

                double lit = -0.6 * nx - 0.8 * ny;
                bool checker = ((centre + dx + r) & 1) == 0;
                int shade;
                if (lit > 0.45)
                {
                    shade = 3;
                }
                else if (lit > 0.1)
                {
                    shade = checker ? 3 : 2;
                }
                else if (lit > -0.25)
                {
                    shade = 2;
                }
                else if (lit > -0.55)
                {
                    shade = checker ? 2 : 1;
                }
                else
                {
                    shade = 1;
                }
                if (ny > 0.55)
                {
                    shade = Math.Max(0, shade - 1);
                }
                Put(data, period, height, centre + dx, top + r, green[shade]);
            }
        }
    }

    static void Put(byte[] data, int period, int height, int x, int y, int rgb)
    {
        if (y < 0 || y >= height)
        {
            return;
        }
        int wrapped = ((x % period) + period) % period;
        int i = (y * period + wrapped) * 4;
        data[i] = (byte)(rgb >> 16);
        data[i + 1] = (byte)((rgb >> 8) & 255);
        data[i + 2] = (byte)(rgb & 255);
        data[i + 3] = 255;
    }

    static int Hash(int a, int b, int c)
    {
        unchecked
        {
            uint h = (uint)(a * 73856093 ^ b * 19349663 ^ c * 83492791);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return (int)(h & 0x7fffffff);
        }
    }
}
