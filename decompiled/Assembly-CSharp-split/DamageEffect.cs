using UnityEngine;

public abstract class DamageEffect : MonoBehaviour
{
	public abstract void DoDamageEffect(Vector2 dmg, bool selfDmg, Player damagedPlayer = null);
}
