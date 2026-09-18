using UnityEngine;

// From PCE
namespace ModdingUtils.RoundsEffects
{
    public abstract class HitSurfaceEffect : MonoBehaviour
    {
        public abstract void Hit(Vector2 position, Vector2 normal, Vector2 velocity);

        public void Destroy()
        {
            UnityEngine.Object.Destroy(this);
        }

    }
}