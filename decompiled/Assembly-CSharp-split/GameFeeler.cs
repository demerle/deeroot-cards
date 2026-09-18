using UnityEngine;

public abstract class GameFeeler : MonoBehaviour
{
	private void Awake()
	{
		GamefeelManager.RegisterGamefeeler(this);
		OnAwake();
	}

	public abstract void OnGameFeel(Vector2 feelDirection);

	public abstract void OnUIGameFeel(Vector2 feelDirection);

	public virtual void OnAwake()
	{
	}
}
