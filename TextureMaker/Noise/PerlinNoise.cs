namespace TextureMaker.Noise;

/// <summary>Standalone Perlin 2D noise implementation.</summary>
public sealed class PerlinNoise
{
    private readonly int[] _perm;

    public PerlinNoise(int seed = 0)
    {
        var rng = new Random(seed);
        _perm = new int[512];
        int[] p = new int[256];
        for (int i = 0; i < 256; i++) p[i] = i;
        for (int i = 255; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (p[i], p[j]) = (p[j], p[i]);
        }
        for (int i = 0; i < 512; i++) _perm[i] = p[i & 255];
    }

    public float Sample(float x, float y)
    {
        int xi = (int)Math.Floor(x) & 255;
        int yi = (int)Math.Floor(y) & 255;
        float xf = x - MathF.Floor(x);
        float yf = y - MathF.Floor(y);
        float u = Fade(xf);
        float v = Fade(yf);

        int aa = _perm[_perm[xi] + yi];
        int ab = _perm[_perm[xi] + yi + 1];
        int ba = _perm[_perm[xi + 1] + yi];
        int bb = _perm[_perm[xi + 1] + yi + 1];

        float x1 = Lerp(Grad(aa, xf, yf),     Grad(ba, xf - 1, yf),     u);
        float x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);
        return (Lerp(x1, x2, v) + 1f) * 0.5f; // remap to [0,1]
    }

    /// <summary>Octave (fractal) noise summed over multiple layers.</summary>
    public float Octave(float x, float y, int octaves, float persistence = 0.5f, float lacunarity = 2f)
    {
        float value = 0f, amplitude = 1f, maxVal = 0f;
        for (int i = 0; i < octaves; i++)
        {
            value   += Sample(x, y) * amplitude;
            maxVal  += amplitude;
            amplitude *= persistence;
            x *= lacunarity;
            y *= lacunarity;
        }
        return value / maxVal;
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static float Lerp(float a, float b, float t) => a + t * (b - a);
    private static float Grad(int hash, float x, float y)
    {
        int h = hash & 3;
        float u = h < 2 ? x : y;
        float v = h < 2 ? y : x;
        return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -v : v);
    }
}
