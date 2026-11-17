using System;
using System.Collections.Generic;

public static class BackgroundLayerSplitter
{
    public const int SourceWidth = 300;
    public const int SourceHeight = 200;

    public const int Sky = 0;
    public const int FarClouds = 1;
    public const int Fog = 2;
    public const int BackMountains = 3;
    public const int CenterMountain = 4;
    public const int Field = 5;
    public const int Trees = 6;
    public const int MaxTreeLayers = 4;
    public static int TreeLayerCount = 4;
    public static int Grass { get { return Trees + TreeLayerCount; } }
    public static int LayerCount { get { return Trees + TreeLayerCount + 1; } }

    public const int Scale = 1;
    public const int FieldPeriod = 840;

    static readonly string[] FixedNames = { "sky", "far_clouds", "fog", "back_mountains", "center_mountain", "field" };
    static readonly string[] TreeNames = { "trees_far", "trees_back", "trees_mid", "trees_front" };

    public static string LayerName(int i)
    {
        if (i < Trees)
        {
            return FixedNames[i];
        }
        if (i < Grass)
        {
            return TreeNames[MaxTreeLayers - TreeLayerCount + (i - Trees)];
        }
        return "grass";
    }

    const int OwnSky = 0;
    const int OwnMoon = 1;
    const int OwnCloud = 2;
    const int OwnBack = 3;
    const int OwnCenter = 4;
    const int OwnFog = 5;
    const int OwnGrass = 6;

    public const int InsertAfterRow = 99;

    const int MoonLastRow = 62;
    const int FlatSkyStart = 100;
    const int FogTop = 166;
    const int CenterLeft = 83;
    const int CenterRight = 210;
    const int LeftPeakX0 = 30;
    const int LeftPeakX1 = 47;
    const int LeftPeakBottom = 172;
    const int RightPeakX0 = 236;
    const int RightPeakX1 = 252;
    const int RightPeakBottom = 169;
    const int CloudGap = 120;
    const int LeftCloudEnd = 116;
    const int RightCloudStart = 208;
    const int FillStep = 8;
    const int FogFillTop = 172;
    const int GrassTopPercent = 3;
    const int GrassSolidPercent = 40;
    const int MountainBaseReach = 30;
    const int MountainMinColumn = 8;
    const int MaxCutRunMirror = 80;

    static int Rgb(int r, int g, int b)
    {
        return (r << 16) | (g << 8) | b;
    }

    static readonly HashSet<int> MoonColors = new HashSet<int>
    {
        Rgb(143, 105, 29), Rgb(176, 129, 35), Rgb(184, 136, 40), Rgb(196, 146, 45), Rgb(232, 175, 60), Rgb(255, 196, 69)
    };

    static readonly HashSet<int> SkyColors = new HashSet<int>
    {
        Rgb(15, 26, 38), Rgb(25, 44, 64), Rgb(43, 73, 107), Rgb(77, 123, 179), Rgb(110, 173, 255)
    };

    static readonly HashSet<int> RockColors = new HashSet<int>
    {
        Rgb(38, 38, 38), Rgb(48, 48, 48), Rgb(133, 133, 133)
    };

    static readonly HashSet<int> GrassColors = new HashSet<int>
    {
        Rgb(41, 54, 12), Rgb(71, 94, 21), Rgb(94, 125, 29), Rgb(112, 148, 35)
    };

    static readonly HashSet<int> LightColors = new HashSet<int>
    {
        Rgb(255, 255, 255), Rgb(228, 228, 228), Rgb(203, 203, 203), Rgb(204, 202, 204), Rgb(181, 181, 181), Rgb(161, 161, 161)
    };

    public class LayerImage
    {
        public string name;
        public int width;
        public int height;
        public int pad;
        public bool mirrored;
        public byte[] rgba;
    }

    public static byte[] InsertRows(byte[] src, int w, int h, int afterRow, int count)
    {
        if (afterRow < 0 || afterRow >= h)
        {
            throw new ArgumentException("afterRow is outside the picture");
        }
        if (count < 0)
        {
            count = 0;
        }

        int first = RowKey(src, w, afterRow, 0);
        for (int x = 1; x < w; x++)
        {
            if (RowKey(src, w, afterRow, x) != first)
            {
                throw new InvalidOperationException("Row " + afterRow + " is not one flat colour, pick another row.");
            }
        }

        int rowBytes = w * 4;
        byte[] dst = new byte[rowBytes * (h + count)];
        for (int y = 0; y < h + count; y++)
        {
            int srcRow = SourceRow(y, afterRow, count);
            Buffer.BlockCopy(src, srcRow * rowBytes, dst, y * rowBytes, rowBytes);
        }
        return dst;
    }

    static int SourceRow(int y, int afterRow, int count)
    {
        if (y <= afterRow)
        {
            return y;
        }
        if (y <= afterRow + count)
        {
            return afterRow;
        }
        return y - count;
    }

    static int RowKey(byte[] a, int w, int y, int x)
    {
        int i = (y * w + x) * 4;
        return (a[i] << 16) | (a[i + 1] << 8) | a[i + 2];
    }

    public class TreeSettings
    {
        public int[] minHeight = { 15, 18, 21, 24 };
        public int[] maxHeight = { 22, 27, 32, 37 };
        public int[] spacing = { 6, 7, 8, 9 };
        public float[] darkness = { 1.00f, 0.92f, 0.82f, 0.72f };
        public float[] haze = { 0.26f, 0.16f, 0.08f, 0f };
        public int seed = 7;
        public int period = 1200;
        public int periodStep = 60;
        public int sink = 1;

        public int measuredPeak;
        public int appliedMax;
        public int groundRowFine;
    }

    public static int ShortestPeak(byte[] back, byte[] center, int w, int h, int grassTop)
    {
        int shortest = int.MaxValue;
        byte[][] pictures = { back, center };
        for (int p = 0; p < pictures.Length; p++)
        {
            int[] tops = new int[w];
            for (int x = 0; x < w; x++)
            {
                tops[x] = int.MaxValue;
                for (int y = 0; y < h; y++)
                {
                    if (pictures[p][(y * w + x) * 4 + 3] != 0)
                    {
                        tops[x] = y;
                        break;
                    }
                }
            }
            for (int x = 0; x < w; x++)
            {
                if (tops[x] == int.MaxValue || grassTop - tops[x] < 4)
                {
                    continue;
                }
                int lowest = int.MaxValue;
                for (int k = Math.Max(0, x - 15); k <= Math.Min(w - 1, x + 15); k++)
                {
                    lowest = Math.Min(lowest, tops[k]);
                }
                if (tops[x] == lowest)
                {
                    shortest = Math.Min(shortest, grassTop - tops[x]);
                }
            }
        }
        return shortest;
    }

    public static LayerImage[] Split(byte[] tall, int w, int h, int insertCount, int periodClouds, TreeSettings trees)
    {
        if (w != SourceWidth || h != SourceHeight + insertCount)
        {
            throw new InvalidOperationException("Expected a " + SourceWidth + " x " + (SourceHeight + insertCount)
                + " picture, got " + w + " x " + h + ".");
        }

        int[] owner = new int[w * h];
        for (int y = 0; y < h; y++)
        {
            int yo = SourceRow(y, InsertAfterRow, insertCount);
            for (int x = 0; x < w; x++)
            {
                owner[y * w + x] = Classify(x, yo, RowKey(tall, w, y, x));
            }
        }

        byte[] sky = BuildSky(tall, w, h, owner);
        byte[] clouds = Extract(tall, w, h, owner, OwnCloud);
        CompleteCutClouds(clouds, owner, w, h);
        FillEnclosedHoles(clouds, w, h);
        byte[] back = Extract(tall, w, h, owner, OwnBack);
        byte[] center = Extract(tall, w, h, owner, OwnCenter);
        byte[] grass = Extract(tall, w, h, owner, OwnGrass);

        int grassTop = FirstGrassRow(grass, w, h, GrassTopPercent);
        int grassSolid = FirstGrassRow(grass, w, h, GrassSolidPercent);
        if (grassTop < 0 || grassSolid < 0)
        {
            throw new InvalidOperationException("No grass found in the picture.");
        }
        byte[] fog = BuildFog(tall, owner, w, h, TallRow(FogFillTop, insertCount));
        FillSolidGrass(grass, w, h, grassSolid);
        CompleteMountainBase(back, owner, w, h, grassTop);
        CompleteMountainBase(center, owner, w, h, grassTop);

        trees.groundRowFine = grassTop * Scale;
        LayerImage[] layers = new LayerImage[LayerCount];

        layers[Sky] = MakeMirrored(LayerName(Sky), sky, w, h);
        layers[FarClouds] = MakeObjectLayer(LayerName(FarClouds), clouds, w, h, periodClouds, true);
        layers[Fog] = MakeMirrored(LayerName(Fog), fog, w, h);
        layers[BackMountains] = MakeMirrored(LayerName(BackMountains), back, w, h);
        layers[CenterMountain] = MakeMirrored(LayerName(CenterMountain), center, w, h);

        int fieldWidth = FieldPeriod * Scale;
        layers[Field] = Looping(LayerName(Field), BackgroundField.Make(fieldWidth, h * Scale, grassTop * Scale, 15), fieldWidth, h * Scale);

        int shortestPeak = ShortestPeak(back, center, w, h, grassTop);
        trees.measuredPeak = shortestPeak;
        int treeCount = Math.Max(0, Math.Min(MaxTreeLayers, TreeLayerCount));
        int firstSetting = MaxTreeLayers - treeCount;
        int tallest = 1;
        for (int j = firstSetting; j < MaxTreeLayers; j++)
        {
            tallest = Math.Max(tallest, trees.maxHeight[j]);
        }
        float heightScale = 1f;
        if (shortestPeak != int.MaxValue)
        {
            int cap = Math.Max(8, shortestPeak - 3);
            if (tallest > cap)
            {
                heightScale = (float)cap / tallest;
            }
        }
        trees.appliedMax = Math.Min(tallest, (int)Math.Floor(tallest * heightScale));
        for (int j = 0; j < treeCount; j++)
        {
            layers[Trees + j] = BackgroundTreeMaker.Make(LayerName(Trees + j), trees, firstSetting + j, SourceWidth * Scale, h * Scale, grassTop * Scale, heightScale);
        }

        layers[Grass] = MakeMirrored(LayerName(Grass), grass, w, h);
        return layers;
    }
    static LayerImage Looping(string name, byte[] rgba, int width, int height)
    {
        return new LayerImage { name = name, width = width, height = height, pad = (width - SourceWidth * Scale) / 2, mirrored = false, rgba = rgba };
    }

    static int TallRow(int originalRow, int insertCount)
    {
        return originalRow > InsertAfterRow ? originalRow + insertCount : originalRow;
    }

    static int Classify(int x, int yo, int key)
    {
        if (GrassColors.Contains(key))
        {
            return OwnGrass;
        }
        if (yo <= MoonLastRow && MoonColors.Contains(key))
        {
            return OwnMoon;
        }
        if (SkyColors.Contains(key))
        {
            return OwnSky;
        }
        if (RockColors.Contains(key))
        {
            return RockLayer(x);
        }
        if (LightColors.Contains(key))
        {
            if (yo < FlatSkyStart)
            {
                return OwnSky;
            }
            if (x >= LeftPeakX0 && x <= LeftPeakX1 && yo <= LeftPeakBottom)
            {
                return OwnBack;
            }
            if (x >= RightPeakX0 && x <= RightPeakX1 && yo <= RightPeakBottom)
            {
                return OwnBack;
            }
            if (yo < FogTop)
            {
                return RockLayer(x);
            }
            return OwnFog;
        }
        if (yo < FlatSkyStart)
        {
            return OwnMoon;
        }
        return yo < FogTop ? OwnCloud : OwnFog;
    }

    static int RockLayer(int x)
    {
        return x >= CenterLeft && x < CenterRight ? OwnCenter : OwnBack;
    }

    static byte[] Extract(byte[] tall, int w, int h, int[] owner, int layer)
    {
        byte[] result = new byte[w * h * 4];
        for (int p = 0; p < w * h; p++)
        {
            if (owner[p] == layer)
            {
                int i = p * 4;
                result[i] = tall[i];
                result[i + 1] = tall[i + 1];
                result[i + 2] = tall[i + 2];
                result[i + 3] = 255;
            }
        }
        return result;
    }

    static byte[] BuildSky(byte[] tall, int w, int h, int[] owner)
    {
        byte[] result = new byte[w * h * 4];
        bool[] known = new bool[w];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                known[x] = owner[y * w + x] == OwnSky && !LightColors.Contains(RowKey(tall, w, y, x));
            }

            for (int x = 0; x < w; x++)
            {
                int dst = (y * w + x) * 4;
                if (owner[y * w + x] == OwnSky)
                {
                    CopyPixel(tall, dst, result, dst);
                    continue;
                }

                int sourceX = -1;
                for (int step = FillStep; ; step += FillStep)
                {
                    int left = x - step;
                    int right = x + step;
                    bool leftInside = left >= 0;
                    bool rightInside = right < w;
                    if (!leftInside && !rightInside)
                    {
                        break;
                    }
                    if (leftInside && known[left])
                    {
                        sourceX = left;
                        break;
                    }
                    if (rightInside && known[right])
                    {
                        sourceX = right;
                        break;
                    }
                }

                if (sourceX >= 0)
                {
                    CopyPixel(tall, (y * w + sourceX) * 4, result, dst);
                }
                else if (y > 0)
                {
                    CopyPixel(result, ((y - 1) * w + x) * 4, result, dst);
                }
                else
                {
                    result[dst] = 15;
                    result[dst + 1] = 26;
                    result[dst + 2] = 38;
                    result[dst + 3] = 255;
                }
            }
        }
        return result;
    }

    static void CopyPixel(byte[] from, int fromIndex, byte[] to, int toIndex)
    {
        to[toIndex] = from[fromIndex];
        to[toIndex + 1] = from[fromIndex + 1];
        to[toIndex + 2] = from[fromIndex + 2];
        to[toIndex + 3] = 255;
    }


    public static int FirstGrassRow(byte[] pix, int w, int h, int percent)
    {
        int needed = Math.Max(1, w * percent / 100);
        for (int y = 0; y < h; y++)
        {
            int count = 0;
            for (int x = 0; x < w; x++)
            {
                if (pix[(y * w + x) * 4 + 3] != 0)
                {
                    count++;
                }
            }
            if (count >= needed)
            {
                return y;
            }
        }
        return -1;
    }

    static void FillEnclosedHoles(byte[] pix, int w, int h)
    {
        bool[] outside = new bool[w * h];
        Stack<int> open = new Stack<int>();
        for (int x = 0; x < w; x++)
        {
            PushOutside(pix, outside, open, w, h, x, 0);
            PushOutside(pix, outside, open, w, h, x, h - 1);
        }
        for (int y = 0; y < h; y++)
        {
            PushOutside(pix, outside, open, w, h, 0, y);
            PushOutside(pix, outside, open, w, h, w - 1, y);
        }
        while (open.Count > 0)
        {
            int p = open.Pop();
            int x = p % w;
            int y = p / w;
            PushOutside(pix, outside, open, w, h, x - 1, y);
            PushOutside(pix, outside, open, w, h, x + 1, y);
            PushOutside(pix, outside, open, w, h, x, y - 1);
            PushOutside(pix, outside, open, w, h, x, y + 1);
        }

        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int p = y * w + x;
                    if (outside[p] || pix[p * 4 + 3] != 0)
                    {
                        continue;
                    }
                    int source = -1;
                    if (x > 0 && pix[(p - 1) * 4 + 3] != 0)
                    {
                        source = p - 1;
                    }
                    else if (y > 0 && pix[(p - w) * 4 + 3] != 0)
                    {
                        source = p - w;
                    }
                    else if (x < w - 1 && pix[(p + 1) * 4 + 3] != 0)
                    {
                        source = p + 1;
                    }
                    else if (y < h - 1 && pix[(p + w) * 4 + 3] != 0)
                    {
                        source = p + w;
                    }
                    if (source >= 0)
                    {
                        CopyPixel(pix, source * 4, pix, p * 4);
                        changed = true;
                    }
                }
            }
        }
    }

    static void PushOutside(byte[] pix, bool[] outside, Stack<int> open, int w, int h, int x, int y)
    {
        if (x < 0 || y < 0 || x >= w || y >= h)
        {
            return;
        }
        int p = y * w + x;
        if (outside[p] || pix[p * 4 + 3] != 0)
        {
            return;
        }
        outside[p] = true;
        open.Push(p);
    }

    static void CompleteCutClouds(byte[] pix, int[] owner, int w, int h)
    {
        List<int> runs = new List<int>();
        for (int y = 0; y < h; y++)
        {
            runs.Clear();
            int x = 0;
            while (x < w)
            {
                if (pix[(y * w + x) * 4 + 3] == 0)
                {
                    x++;
                    continue;
                }
                int start = x;
                while (x < w && pix[(y * w + x) * 4 + 3] != 0)
                {
                    x++;
                }
                runs.Add(start);
                runs.Add(x - 1);
            }

            for (int r = 0; r < runs.Count; r += 2)
            {
                int left = runs[r];
                int right = runs[r + 1];
                int length = right - left + 1;
                bool coveredLeft = left > 0 && IsRock(owner[y * w + left - 1]);
                bool coveredRight = right < w - 1 && IsRock(owner[y * w + right + 1]);
                for (int k = 1; k <= Math.Min(length, MaxCutRunMirror); k++)
                {
                    if (coveredLeft && left - k >= 0 && pix[(y * w + left - k) * 4 + 3] == 0)
                    {
                        CopyPixel(pix, (y * w + left + k - 1) * 4, pix, (y * w + left - k) * 4);
                    }
                    if (coveredRight && right + k < w && pix[(y * w + right + k) * 4 + 3] == 0)
                    {
                        CopyPixel(pix, (y * w + right - k + 1) * 4, pix, (y * w + right + k) * 4);
                    }
                }
            }
        }
    }

    static bool IsRock(int own)
    {
        return own == OwnBack || own == OwnCenter;
    }

    static void CompleteMountainBase(byte[] pix, int[] owner, int w, int h, int grassTop)
    {
        for (int x = 0; x < w; x++)
        {
            int top = -1;
            int bottom = -1;
            int filled = 0;
            for (int y = 0; y < h; y++)
            {
                if (pix[(y * w + x) * 4 + 3] != 0)
                {
                    if (top < 0)
                    {
                        top = y;
                    }
                    bottom = y;
                    filled++;
                }
            }
            if (bottom < 0 || filled < MountainMinColumn || bottom < grassTop - MountainBaseReach)
            {
                continue;
            }

            int last = top;
            for (int y = top; y <= bottom; y++)
            {
                int i = (y * w + x) * 4;
                if (pix[i + 3] != 0)
                {
                    last = y;
                }
                else if (owner[y * w + x] == OwnGrass)
                {
                    CopyPixel(pix, (last * w + x) * 4, pix, i);
                }
            }
            for (int y = bottom + 1; y < h; y++)
            {
                CopyPixel(pix, (bottom * w + x) * 4, pix, (y * w + x) * 4);
            }
        }
    }

    static byte[] BuildFog(byte[] tall, int[] owner, int w, int h, int fillTop)
    {
        byte[] result = Extract(tall, w, h, owner, OwnFog);
        for (int y = fillTop; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int own = owner[y * w + x];
                if (own != OwnBack && own != OwnCenter && own != OwnGrass)
                {
                    continue;
                }

                int source = -1;
                for (int d = 1; d < w; d++)
                {
                    if (x - d >= 0 && owner[y * w + x - d] == OwnFog)
                    {
                        source = y * w + x - d;
                        break;
                    }
                    if (x + d < w && owner[y * w + x + d] == OwnFog)
                    {
                        source = y * w + x + d;
                        break;
                    }
                }
                int dst = (y * w + x) * 4;
                if (source >= 0)
                {
                    CopyPixel(tall, source * 4, result, dst);
                }
                else if (y > 0 && result[((y - 1) * w + x) * 4 + 3] != 0)
                {
                    CopyPixel(result, ((y - 1) * w + x) * 4, result, dst);
                }
            }
        }
        return result;
    }

    public static void FillSolidGrass(byte[] pix, int w, int h, int solidRow)
    {
        byte[] original = (byte[])pix.Clone();
        for (int y = solidRow; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int dst = (y * w + x) * 4;
                if (original[dst + 3] != 0)
                {
                    continue;
                }
                for (int d = 1; d <= 64; d++)
                {
                    int cells = 8 * d;
                    int start = (int)((uint)(x * 73856093 ^ y * 19349663 ^ d * 83492791) % (uint)cells);
                    bool found = false;
                    for (int c = 0; c < cells && !found; c++)
                    {
                        int k = (start + c) % cells;
                        int side = k / (2 * d);
                        int t = k % (2 * d);
                        int dx;
                        int dy;
                        if (side == 0)
                        {
                            dx = -d + t;
                            dy = -d;
                        }
                        else if (side == 1)
                        {
                            dx = d;
                            dy = -d + t;
                        }
                        else if (side == 2)
                        {
                            dx = d - t;
                            dy = d;
                        }
                        else
                        {
                            dx = -d;
                            dy = d - t;
                        }
                        int sx = x + dx;
                        int sy = y + dy;
                        if (sx < 0 || sy < 0 || sx >= w || sy >= h || sy < solidRow - 2)
                        {
                            continue;
                        }
                        int src = (sy * w + sx) * 4;
                        if (original[src + 3] != 0)
                        {
                            CopyPixel(original, src, pix, dst);
                            found = true;
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }
            }
        }
    }

    public static LayerImage MakeMirrored(string name, byte[] pix, int w, int h)
    {
        int width = 2 * w - 2;
        byte[] data = new byte[width * h * 4];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int sx = x < w ? x : 2 * w - 2 - x;
                Buffer.BlockCopy(pix, (y * w + sx) * 4, data, (y * width + x) * 4, 4);
            }
        }
        return new LayerImage { name = name, width = width, height = h, pad = 0, mirrored = true, rgba = data };
    }
    static LayerImage MakeObjectLayer(string name, byte[] pix, int w, int h, int minPeriod, bool extend)
    {
        int leftExtra = 0;
        int rightExtra = 0;
        if (extend)
        {
            while (leftExtra < LeftCloudEnd && ColumnHasPixels(pix, w, h, leftExtra))
            {
                leftExtra++;
            }
            while (rightExtra < w - RightCloudStart && ColumnHasPixels(pix, w, h, w - 1 - rightExtra))
            {
                rightExtra++;
            }
        }

        int ew = leftExtra + w + rightExtra;
        byte[] ext = new byte[ew * h * 4];
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        for (int y = 0; y < h; y++)
        {
            for (int e = 0; e < ew; e++)
            {
                int x = e - leftExtra;
                int sx;
                if (x < 0)
                {
                    sx = -x - 1;
                }
                else if (x >= w)
                {
                    sx = 2 * w - 1 - x;
                }
                else
                {
                    sx = x;
                }

                int s = (y * w + sx) * 4;
                if (pix[s + 3] == 0)
                {
                    continue;
                }
                Buffer.BlockCopy(pix, s, ext, (y * ew + e) * 4, 4);
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
            }
        }
        if (maxX < minX)
        {
            minX = 0;
            maxX = w - 1;
        }

        int content = maxX - minX + 1;
        int period = Math.Max(minPeriod, content + 2 * CloudGap);
        int pad = period / 2 - (minX + maxX) / 2;
        if (pad + minX < 0 || pad + maxX >= period || pad < 0 || pad + w > period)
        {
            throw new InvalidOperationException(name + ": the period " + period + " is too small for the picture.");
        }

        byte[] data = new byte[period * h * 4];
        for (int y = 0; y < h; y++)
        {
            for (int e = 0; e < ew; e++)
            {
                int x = e - leftExtra;
                int target = x + pad;
                if (target < 0 || target >= period)
                {
                    continue;
                }
                Buffer.BlockCopy(ext, (y * ew + e) * 4, data, (y * period + target) * 4, 4);
            }
        }
        return new LayerImage { name = name, width = period, height = h, pad = pad, mirrored = false, rgba = data };
    }

    static bool ColumnHasPixels(byte[] pix, int w, int h, int x)
    {
        for (int y = 0; y < h; y++)
        {
            if (pix[(y * w + x) * 4 + 3] != 0)
            {
                return true;
            }
        }
        return false;
    }
    public static byte[] BuildPreview(LayerImage[] layers, out int width, out int height)
    {
        const int margin = 8;
        const int square = 8;
        width = 0;
        height = margin;
        for (int l = 0; l < layers.Length; l++)
        {
            width = Math.Max(width, layers[l].width);
            height += layers[l].height + margin;
        }
        width += 2 * margin;

        byte[] data = new byte[width * height * 4];
        for (int p = 0; p < width * height; p++)
        {
            data[p * 4] = 40;
            data[p * 4 + 1] = 40;
            data[p * 4 + 2] = 48;
            data[p * 4 + 3] = 255;
        }

        int top = margin;
        for (int l = 0; l < layers.Length; l++)
        {
            LayerImage layer = layers[l];
            for (int y = 0; y < layer.height; y++)
            {
                for (int x = 0; x < layer.width; x++)
                {
                    int d = ((top + y) * width + margin + x) * 4;
                    int shade = ((x / square) + (y / square)) % 2 == 0 ? 200 : 150;
                    int s = (y * layer.width + x) * 4;
                    int alpha = layer.rgba[s + 3];
                    for (int c = 0; c < 3; c++)
                    {
                        data[d + c] = (byte)(alpha == 0 ? shade : layer.rgba[s + c]);
                    }
                    data[d + 3] = 255;
                }
            }
            top += layer.height + margin;
        }
        return data;
    }
    public static void DominantRowColor(byte[] rgba, int width, int row, out byte r, out byte g, out byte b)
    {
        Dictionary<int, int> counts = new Dictionary<int, int>();
        int best = 0;
        int bestCount = -1;
        for (int x = 0; x < width; x++)
        {
            int i = (row * width + x) * 4;
            if (rgba[i + 3] == 0)
            {
                continue;
            }
            int key = (rgba[i] << 16) | (rgba[i + 1] << 8) | rgba[i + 2];
            int count;
            counts.TryGetValue(key, out count);
            count++;
            counts[key] = count;
            if (count > bestCount)
            {
                bestCount = count;
                best = key;
            }
        }
        r = (byte)(best >> 16);
        g = (byte)((best >> 8) & 255);
        b = (byte)(best & 255);
    }
}
