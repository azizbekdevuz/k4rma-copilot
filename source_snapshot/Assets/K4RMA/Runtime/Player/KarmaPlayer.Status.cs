using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaPlayer
    {
        float stunUntil;
        bool confined;
        float arenaLeft, arenaRight;
        public bool Stunned { get { return Time.time < stunUntil; } }
        public int ParryCount { get; private set; }
        public void Stun(float seconds)
        {
            if (!game.IsCombat || Health <= 0 || Stunned) return;
            CancelAirSlash(); move = 0; jumpBufferedUntil = 0;
            stunUntil = Time.time + Mathf.Max(0, seconds);
            // A complete recovery interval before another hit is allowed.
            invulnerableUntil = Mathf.Max(invulnerableUntil, stunUntil + 0.25f);
            body.linearVelocity = Vector2.zero;
            techniques.ClearDirectionTap();
        }
        public void ConfineArena(float left, float right)
        {
            CancelAirSlash(); confined = true; arenaLeft = left; arenaRight = right;
            Vector2 p = new Vector2(Mathf.Clamp(Position.x, left + KarmaSpace.Units(1), right - KarmaSpace.Units(1)), Mathf.Clamp(Position.y, KarmaSpace.Units(1), KarmaSpace.Units(6)));
            body.position = p; transform.position = p; body.linearVelocity = Vector2.zero;
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + 0.6f);
        }
        public void ReleaseArena() { confined = false; }
        void LateUpdate()
        {
            if (!confined || game == null || game.Paused) return;
            float x = Mathf.Clamp(body.position.x, arenaLeft, arenaRight);
            if (Mathf.Abs(x - body.position.x) < 0.0001f) return;
            Vector2 p = new Vector2(x, body.position.y);
            body.position = p; transform.position = p;
            body.linearVelocity = new Vector2(0, body.linearVelocity.y);
        }
        public void ResolveBarrier(float centerX, float width, float top = float.PositiveInfinity)
        {
            if (Position.y - KarmaSpace.PlayerHalfHeight >= top - 0.02f) return;
            float separation = width / 2 + KarmaSpace.PlayerHalfWidth + KarmaSpace.Units(0.05f);
            if (Mathf.Abs(Position.x - centerX) >= separation) return;
            CancelAirSlash();
            if (!float.IsPositiveInfinity(top))
            {
                // Adjacent rising steps must lift, rather than shove into an already solid neighbor.
                Vector2 above = new Vector2(Position.x, top + KarmaSpace.PlayerHalfHeight + KarmaSpace.Units(0.03f));
                body.position = above; transform.position = above; body.linearVelocity = Vector2.zero;
                return;
            }
            Vector2 p = new Vector2(centerX + (Position.x < centerX ? -separation : separation), Position.y);
            body.position = p; transform.position = p;
            body.linearVelocity = new Vector2(0, body.linearVelocity.y);
        }
        public void ClearStatus()
        {
            CancelAirSlash(); stunUntil = 0; ReleaseArena();
        }
    }
}
