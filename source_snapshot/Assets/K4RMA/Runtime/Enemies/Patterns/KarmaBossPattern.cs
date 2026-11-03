using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        abstract class BossPattern
        {
            protected readonly KarmaEnemy owner;
            protected BossPattern(KarmaEnemy owner) { this.owner = owner; }
            public abstract IEnumerator Execute();
            public virtual bool CanReach(Vector2 from, Vector2 target) { return true; }
            public virtual void Interrupt() { }
        }
    }
}
