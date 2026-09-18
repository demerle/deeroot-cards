using UnityEngine;

// From PCE
namespace ModdingUtils.RoundsEffects
{
    // this is distinct from DealtDamageEffect since it will not trigger on DamageOverTime
    public abstract class HitEffect : MonoBehaviour
    {
        public abstract void DealtDamage(Vector2 damage, bool selfDamage, Player damagedPlayer = null);

        public void Destroy()
        {
            UnityEngine.Object.Destroy(this);
        }

    }
}