using UnityEngine;
using UnityEngine.Events;

public class DestroyEvent : MonoBehaviour
{
	public UnityEvent deathEvent;

	private bool m_isQuitting;

	private void OnDestroy()
	{
		if (!m_isQuitting)
		{
			deathEvent.Invoke();
		}
	}

	private void OnApplicationQuit()
	{
		m_isQuitting = true;
	}
}
