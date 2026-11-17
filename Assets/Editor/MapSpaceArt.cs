using System;

public static class MapSpaceArt
{
    public const int GradientWidth = 512;
    public const double GradientUnit = 0.16;
    public const double GradientBottom = 0.0;
    public const double GradientTop = 260.0;

    public static int GradientRows()
    {
        return (int)Math.Round((GradientTop - GradientBottom) / GradientUnit);
    }

    static readonly double[] StopY = { 0, 40, 60, 80, 110, 160, 260 };
    static readonly int[] StopColour = { 0x6CA8FF, 0x6CA8FF, 0x2A4A8F, 0x0E1A3F, 0x080C24, 0x04050F, 0x030308 };

    static int Mix(int a, int b, double t)
    {
        int r = (int)Math.Round(((a >> 16) & 255) * (1 - t) + ((b >> 16) & 255) * t);
        int g = (int)Math.Round(((a >> 8) & 255) * (1 - t) + ((b >> 8) & 255) * t);
        int bl = (int)Math.Round((a & 255) * (1 - t) + (b & 255) * t);
        return (r << 16) | (g << 8) | bl;
    }

    static int GradientAt(double y)
    {
        if (y <= StopY[0])
        {
            return StopColour[0];
        }
        for (int i = 0; i + 1 < StopY.Length; i++)
        {
            if (y <= StopY[i + 1])
            {
                return Mix(StopColour[i], StopColour[i + 1], (y - StopY[i]) / (StopY[i + 1] - StopY[i]));
            }
        }
        return StopColour[StopColour.Length - 1];
    }

    public static byte[] SpaceGradient()
    {
        int w = GradientWidth;
        int h = GradientRows();
        byte[] data = new byte[w * h * 4];
        for (int row = 0; row < h; row++)
        {
            int colour = GradientAt(GradientBottom + (h - 1 - row) * GradientUnit);
            for (int x = 0; x < w; x++)
            {
                int i = (row * w + x) * 4;
                data[i] = (byte)(colour >> 16);
                data[i + 1] = (byte)((colour >> 8) & 255);
                data[i + 2] = (byte)(colour & 255);
                data[i + 3] = 255;
            }
        }
        return data;
    }
}
