using System;
using UnityEngine;

namespace PickleballXR
{
    public enum SurfaceType { Floor, Kitchen, Net, NetPost, ReboundWall, WallTarget, EndButton }

    /// <summary>
    /// Added automatically to the court's colliders by the setup tool. Raises a static event when a ball
    /// touches it, so HUD code (wall-touch counter, bounce stats, fault detection) needs no per-object wiring.
    /// The ball needs a Rigidbody and a collider, and its GameObject tag must equal <see cref="ballTag"/>
    /// (leave ballTag empty to accept any rigidbody).
    /// </summary>
    [DisallowMultipleComponent]
    public class PickleballSurface : MonoBehaviour
    {
        public SurfaceType surface;
        public string ballTag = "Ball";

        /// <summary>(surface that was hit, collision data)</summary>
        public static event Action<PickleballSurface, Collision> BallHit;

        private void OnCollisionEnter(Collision collision)
        {
            if (!string.IsNullOrEmpty(ballTag) && collision.gameObject.tag != ballTag) return;
            if (BallHit != null) BallHit(this, collision);
        }
    }
}
