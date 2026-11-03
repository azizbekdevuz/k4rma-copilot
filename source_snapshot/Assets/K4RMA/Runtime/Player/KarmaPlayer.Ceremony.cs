using UnityEngine;

namespace KarmaPrototype
{
    public sealed partial class KarmaPlayer
    {
        bool separatingForCeremony;
        float ceremonyMove, ceremonyTargetX, ceremonyGuardianX, ceremonyMoveLeft;
        public bool SeparatingForCeremony => separatingForCeremony;

        public void FaceGuardian(float x)
        {
            if (Mathf.Abs(x - Position.x) > 0.01f) Facing = x < Position.x ? -1 : 1;
            art.localScale = new Vector3(Facing, 1, 1);
        }
        public void BeginCeremonySpacing(Bounds guardianBounds, float guardianX)
        {
            ceremonyGuardianX = guardianX;
            float padding = KarmaSpace.PlayerHalfWidth + KarmaSpace.Units(0.65f);
            float left = Mathf.Min(guardianX - KarmaSpace.Units(3.5f), guardianBounds.min.x - padding);
            float right = Mathf.Max(guardianX + KarmaSpace.Units(3.5f), guardianBounds.max.x + padding);
            if (Position.x <= left || Position.x >= right) { FinishCeremonySpacing(); return; }
            float min = KarmaStage.ArenaLeft + KarmaSpace.PlayerHalfWidth + 0.2f;
            float max = KarmaStage.ArenaRight - KarmaSpace.PlayerHalfWidth - 0.2f;
            bool canLeft = left >= min && ClearCeremonyPath(left);
            bool canRight = right <= max && ClearCeremonyPath(right);
            if (!canLeft && !canRight) { FinishCeremonySpacing(); return; }
            ceremonyTargetX = canLeft && (!canRight || Mathf.Abs(Position.x-left) <= Mathf.Abs(Position.x-right)) ? left : right;
            ceremonyMoveLeft = Mathf.Abs(ceremonyTargetX-Position.x) / Mathf.Max(1, game.Config.moveSpeed * 0.55f) + 0.6f;
            separatingForCeremony = true;
        }
        bool ClearCeremonyPath(float target)
        {
            float distance = Mathf.Abs(target-Position.x);
            foreach (var hit in Physics2D.BoxCastAll(body.position, KarmaSpace.PlayerSize * 0.95f, 0,
                target < Position.x ? Vector2.left : Vector2.right, distance))
                if (hit.collider.GetComponent<KarmaSurface>() != null
                    && hit.collider.GetComponent<PlatformEffector2D>() == null && hit.distance < distance-0.05f)
                    return false;
            return true;
        }
        void UpdateCeremonySpacing()
        {
            ceremonyMoveLeft -= Time.fixedDeltaTime;
            float dx = ceremonyTargetX-body.position.x;
            if (Mathf.Abs(dx) < 0.04f || ceremonyMoveLeft <= 0)
            { FinishCeremonySpacing(); return; }
            ceremonyMove = Mathf.Sign(dx);
            FaceGuardian(ceremonyTargetX);
            body.gravityScale = game.Config.gravityScale;
            body.linearVelocity = new Vector2(ceremonyMove * Mathf.Min(game.Config.moveSpeed * 0.55f,
                Mathf.Abs(dx) / Time.fixedDeltaTime), Mathf.Min(0, body.linearVelocity.y));
        }
        void FinishCeremonySpacing()
        {
            separatingForCeremony = false; ceremonyMove = 0;
            body.linearVelocity = Vector2.zero;
            FaceGuardian(ceremonyGuardianX);
        }
    }
}
