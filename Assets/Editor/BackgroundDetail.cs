using System;

public static class BackgroundDetail
{
    public const int Scale = 2;
    public static int Hash(int a, int b, int c)
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

    public static double Rand(int a, int b, int c)
    {
        return (Hash(a, b, c) % 100003) / 100003.0;
    }

    public static double Bayer(int x, int y)
    {
        int[] m = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
        return (m[(y & 3) * 4 + (x & 3)] + 0.5) / 16.0;
    }

    public static void SetPixel(byte[] data, int width, int x, int y, int rgb)
    {
        int i = (y * width + x) * 4;
        data[i] = (byte)(rgb >> 16);
        data[i + 1] = (byte)((rgb >> 8) & 255);
        data[i + 2] = (byte)(rgb & 255);
        data[i + 3] = 255;
    }

    static bool SamePixel(byte[] d, int a, int b)
    {
        if (d[a + 3] == 0 && d[b + 3] == 0)
        {
            return true;
        }
        return d[a] == d[b] && d[a + 1] == d[b + 1] && d[a + 2] == d[b + 2] && d[a + 3] == d[b + 3];
    }

    public static byte[] Upscale2x(byte[] src, int w, int h, bool wrapX, bool smoothDiagonals)
    {
        int W = w * 2;
        byte[] dst = new byte[W * h * 2 * 4];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int p = (y * w + x) * 4;
                int xl = x > 0 ? x - 1 : (wrapX ? w - 1 : 0);
                int xr = x < w - 1 ? x + 1 : (wrapX ? 0 : w - 1);
                int yu = y > 0 ? y - 1 : 0;
                int yd = y < h - 1 ? y + 1 : h - 1;
                int a = (yu * w + x) * 4;
                int b = (y * w + xr) * 4;
                int c = (y * w + xl) * 4;
                int d = (yd * w + x) * 4;

                int ul = p;
                int ur = p;
                int ll = p;
                int lr = p;
                if (smoothDiagonals)
                {
                    if (SamePixel(src, c, a) && !SamePixel(src, c, d) && !SamePixel(src, a, b))
                    {
                        ul = a;
                    }
                    if (SamePixel(src, a, b) && !SamePixel(src, a, c) && !SamePixel(src, b, d))
                    {
                        ur = b;
                    }
                    if (SamePixel(src, d, c) && !SamePixel(src, d, b) && !SamePixel(src, c, a))
                    {
                        ll = c;
                    }
                    if (SamePixel(src, b, d) && !SamePixel(src, b, a) && !SamePixel(src, d, c))
                    {
                        lr = d;
                    }
                }
                Buffer.BlockCopy(src, ul, dst, ((2 * y) * W + 2 * x) * 4, 4);
                Buffer.BlockCopy(src, ur, dst, ((2 * y) * W + 2 * x + 1) * 4, 4);
                Buffer.BlockCopy(src, ll, dst, ((2 * y + 1) * W + 2 * x) * 4, 4);
                Buffer.BlockCopy(src, lr, dst, ((2 * y + 1) * W + 2 * x + 1) * 4, 4);
            }
        }
        return dst;
    }
}
