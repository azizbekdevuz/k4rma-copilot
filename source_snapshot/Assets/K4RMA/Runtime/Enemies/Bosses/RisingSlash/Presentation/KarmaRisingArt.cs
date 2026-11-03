using UnityEngine;
namespace KarmaPrototype
{
    // Logical rising-effect names resolve through the same serialized library as all other artwork.
    public static class KarmaRisingArt
    {
        public static Sprite Get(string pose) { return KarmaArtwork.Sprite("RisingBossVFX", pose); }
    }
}
