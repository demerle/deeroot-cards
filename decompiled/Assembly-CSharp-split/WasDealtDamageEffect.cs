using UnityEngine;

public abstract class WasDealtDamageEffect : MonoBehaviour
{
	public abstract void WasDealtDamage(Vector2 damage, bool selfDamage);
}
