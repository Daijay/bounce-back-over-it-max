using System;
using System.Collections.Generic;

public static class MapMinimal
{
    public static bool Enabled = true;

    struct Rule
    {
        public int colors;
        public int radius;
        public bool wrapX;
        public bool wrapY;
        public int alphaLevels;
        public int minDistance;
    }

    static bool TryRule(string name, out Rule rule)
    {
        rule = new Rule();
        if (name == "rock")
        {
            rule = new Rule { colors = 2, radius = 3, wrapX = true, wrapY = true, minDistance = 40 };
        }
        else if (name.StartsWith("lava_"))
        {
            rule = new Rule { colors = 1, radius = 1, wrapX = true, wrapY = true };
        }
        else if (name.StartsWith("boulder_"))
        {
            rule = new Rule { colors = 2, radius = 2, minDistance = 40 };
        }
        else if (name == "ground_field")
        {
            rule = new Rule { colors = 6, radius = 3, minDistance = 55 };
        }
        else if (name == "snow")
        {
            rule = new Rule { colors = 1, radius = 1, wrapX = true, wrapY = true };
        }
        else if (name == "golden_chest")
        {
            rule = new Rule { colors = 5, radius = 1 };
        }
        else if (name == "cheese_moon")
        {
            rule = new Rule { colors = 3, radius = 2 };
        }
        else if (name.StartsWith("asteroid_"))
        {
            rule = new Rule { colors = 3, radius = 1 };
        }
        else if (name.StartsWith("planet_"))
        {
            rule = new Rule { colors = 4, radius = 0 };
        }
        else if (name.StartsWith("nebula_") || name == "milky_way")
        {
            rule = new Rule { colors = 3, radius = 0, alphaLevels = 3 };
        }
        else if (name == "space_gradient")
        {
            rule = new Rule { colors = 12, radius = 2, wrapX = true };
        }
        else
        {
            return false;
        }
        return true;
    }

    public static byte[] Apply(string name, byte[] rgba, int width, int height)
    {
        Rule rule;
        if (!Enabled || rgba == null || rgba.Length != width * height * 4 || !TryRule(name, out rule))
        {
            return rgba;
        }
        int radius = rule.radius > 0 ? rule.radius : (Math.Max(width, height) >= 200 ? 2 : 1);
        byte[] data = (byte[])rgba.Clone();
        int count = width * height;

        if (rule.alphaLevels > 0)
        {
            int[] level = new int[count];
            for (int i = 0; i < count; i++)
            {
                level[i] = (int)Math.Round(data[i * 4 + 3] * (rule.alphaLevels / 255.0));
            }
            level = Majority(level, width, height, radius, rule.wrapX, rule.wrapY, rule.alphaLevels + 1, 2);
            for (int i = 0; i < count; i++)
            {
                data[i * 4 + 3] = (byte)Math.Min(255, (int)Math.Round(level[i] * (255.0 / rule.alphaLevels)));
            }
        }

        Dictionary<int, int> usage = new Dictionary<int, int>();
        for (int i = 0; i < count; i++)
        {
            if (data[i * 4 + 3] == 0)
            {
                continue;
            }
            int rgb = (data[i * 4] << 16) | (data[i * 4 + 1] << 8) | data[i * 4 + 2];
            int n;
            usage.TryGetValue(rgb, out n);
            usage[rgb] = n + 1;
        }
        if (usage.Count == 0)
        {
            return data;
        }
        List<KeyValuePair<int, int>> sorted = new List<KeyValuePair<int, int>>(usage);
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
        List<int> chosen = new List<int>();
        long minSquared = (long)rule.minDistance * rule.minDistance;
        for (int k = 0; k < sorted.Count && chosen.Count < rule.colors; k++)
        {
            int colour = sorted[k].Key;
            bool far = true;
            for (int c = 0; c < chosen.Count && far; c++)
            {
                int dr = (colour >> 16) - (chosen[c] >> 16);
                int dg = ((colour >> 8) & 255) - ((chosen[c] >> 8) & 255);
                int db = (colour & 255) - (chosen[c] & 255);
                far = dr * dr + dg * dg + db * db >= minSquared;
            }
            if (far)
            {
                chosen.Add(colour);
            }
        }
        int kept = chosen.Count;
        int[] palette = chosen.ToArray();

        int[] index = new int[count];
        Dictionary<int, int> cache = new Dictionary<int, int>();
        for (int i = 0; i < count; i++)
        {
            if (data[i * 4 + 3] == 0)
            {
                index[i] = -1;
                continue;
            }
            int rgb = (data[i * 4] << 16) | (data[i * 4 + 1] << 8) | data[i * 4 + 2];
            int found;
            if (!cache.TryGetValue(rgb, out found))
            {
                found = Nearest(palette, rgb);
                cache[rgb] = found;
            }
            index[i] = found;
        }

        index = Majority(index, width, height, radius, rule.wrapX, rule.wrapY, kept, 2);

        for (int i = 0; i < count; i++)
        {
            if (index[i] < 0)
            {
                continue;
            }
            int rgb = palette[index[i]];
            data[i * 4] = (byte)(rgb >> 16);
            data[i * 4 + 1] = (byte)((rgb >> 8) & 255);
            data[i * 4 + 2] = (byte)(rgb & 255);
        }
        return data;
    }

    static int Nearest(int[] palette, int rgb)
    {
        int r = rgb >> 16;
        int g = (rgb >> 8) & 255;
        int b = rgb & 255;
        int best = 0;
        long bestDistance = long.MaxValue;
        for (int k = 0; k < palette.Length; k++)
        {
            int dr = (palette[k] >> 16) - r;
            int dg = ((palette[k] >> 8) & 255) - g;
            int db = (palette[k] & 255) - b;
            long d = dr * dr + dg * dg + db * db;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = k;
            }
        }
        return best;
    }

    static int[] Majority(int[] value, int width, int height, int radius, bool wrapX, bool wrapY, int kinds, int passes)
    {
        int[] current = value;
        int[] votes = new int[kinds];
        for (int pass = 0; pass < passes; pass++)
        {
            int[] next = new int[current.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int own = current[y * width + x];
                    if (own < 0)
                    {
                        next[y * width + x] = own;
                        continue;
                    }
                    Array.Clear(votes, 0, votes.Length);
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int yy = y + dy;
                        if (yy < 0 || yy >= height)
                        {
                            if (!wrapY)
                            {
                                continue;
                            }
                            yy = (yy % height + height) % height;
                        }
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int xx = x + dx;
                            if (xx < 0 || xx >= width)
                            {
                                if (!wrapX)
                                {
                                    continue;
                                }
                                xx = (xx % width + width) % width;
                            }
                            int v = current[yy * width + xx];
                            if (v >= 0 && v < kinds)
                            {
                                votes[v]++;
                            }
                        }
                    }
                    int best = own;
                    for (int k = 0; k < kinds; k++)
                    {
                        if (votes[k] > votes[best])
                        {
                            best = k;
                        }
                    }
                    next[y * width + x] = best;
                }
            }
            current = next;
        }
        return current;
    }
}
