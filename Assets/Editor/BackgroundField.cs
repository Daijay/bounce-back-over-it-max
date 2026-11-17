using System;

public static class BackgroundField
{
    public const int Base = 7;
    public const int Amp = 2;

    static readonly int[] Greens = { 0x29360C, 0x475E15, 0x5E7D1D, 0x709423 };
    static readonly int[] Haze = { 0x5A8658, 0x6E9A66 };

    public static byte[] Make(int width, int height, int groundFine, int seed)
    {
        byte[] data = new byte[width * height * 4];
        int scale = BackgroundLayerSplitter.Scale;
        double phase1 = BackgroundDetail.Rand(1, seed, 0) * Math.PI * 2.0;
        double phase2 = BackgroundDetail.Rand(2, seed, 0) * Math.PI * 2.0;
        double phase3 = BackgroundDetail.Rand(3, seed, 0) * Math.PI * 2.0;
        double twoPi = Math.PI * 2.0;

        double[] rise = new double[width];
        int[] edge = new int[width];
        for (int x = 0; x < width; x++)
        {
            double a = twoPi * x / width;
            double hills = 0.55 * Math.Sin(2.0 * a + phase1) + 0.30 * Math.Sin(5.0 * a + phase2) + 0.15 * Math.Sin(11.0 * a + phase3);
            rise[x] = scale * Base + scale * Amp * hills;
            int unevenness = BackgroundDetail.Rand(x, 2, seed) < 0.3 ? 1 : 0;
            edge[x] = groundFine - (int)Math.Round(rise[x]) - unevenness;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int top = edge[x];
                if (y < top)
                {
                    continue;
                }
                int depth = y - top;
                double a = twoPi * x / width;
                double slope = rise[(x + 3) % width] - rise[(x - 3 + width) % width];

                double n = Math.Sin(3.0 * a + 0.018 * y + phase1) + 0.6 * Math.Sin(7.0 * a - 0.022 * y + phase2)
                    + 0.4 * Math.Sin(13.0 * a + 0.01 * y + phase3);
                double nearTop = Math.Max(0.0, 1.0 - depth / 8.0);
                double light = Math.Max(-1.0, Math.Min(1.0, slope * 0.12)) * 0.6 * nearTop;
                double v = 2.0 + 0.55 * n + light - Math.Min(0.8, depth * 0.012);
                int lo = (int)Math.Floor(v);
                double fraction = Math.Round((v - lo) * 2.0) / 2.0;
                int tone = fraction >= 1.0 ? lo + 1 : (fraction > 0.0 && BackgroundDetail.Bayer(x, y) < 0.5 ? lo + 1 : lo);

                if (BackgroundDetail.Rand(x / 2, y / 2, seed + 5) < 0.025)
                {
                    tone -= 1;
                }
                else if (BackgroundDetail.Rand(x, y, seed + 6) < 0.012)
                {
                    tone += 1;
                }
                tone = Math.Max(0, Math.Min(3, tone));
                int rgb = Greens[tone];

                if (depth < 4)
                {
                    double probability = (4 - depth) / 4.0 * 0.55;
                    if (BackgroundDetail.Bayer(x, y + 1) < probability)
                    {
                        rgb = Haze[(x + y) & 1];
                    }
                }
                BackgroundDetail.SetPixel(data, width, x, y, rgb);
            }
        }

        for (int x = 0; x < width; x++)
        {
            if (BackgroundDetail.Rand(x, 7, seed) >= 0.16)
            {
                continue;
            }
            int length = 1 + BackgroundDetail.Hash(x, 8, seed) % 3;
            for (int k = 1; k <= length; k++)
            {
                int y = edge[x] - k;
                if (y >= 0)
                {
                    BackgroundDetail.SetPixel(data, width, x, y, Greens[2 + (BackgroundDetail.Hash(x, k, seed) & 1)]);
                }
            }
        }
        return data;
    }
}
