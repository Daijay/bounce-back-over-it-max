using UnityEngine;

public class MapSettings : MonoBehaviour
{
    [Tooltip("Seed of the deterministic random that bumps the rock surfaces. The same seed gives the same map.")]
    public int seed = 12345;

    [Tooltip("Scales the surface bumps, steps and ledges (about 0.2 to 0.5 world units). 0 = smooth rock.")]
    public float bumpiness = 1f;
}
