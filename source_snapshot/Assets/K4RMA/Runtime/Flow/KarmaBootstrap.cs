using UnityEngine;
namespace KarmaPrototype
{
    public static class KarmaBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartEmptyScene()
        {
            if (Object.FindAnyObjectByType<KarmaGame>() != null) return;
            new GameObject("K4RMA Game").AddComponent<KarmaGame>();
        }
    }
}
