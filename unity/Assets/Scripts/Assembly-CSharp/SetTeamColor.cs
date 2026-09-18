using UnityEngine;
using UnityEngine.Events;

public class SetTeamColor : MonoBehaviour
{
	public enum ColorType
	{
		Main = 0,
		Background = 1,
		Particle = 2,
		WinText = 3
	}

	private SpriteRenderer m_spriteRenderer;

	private ParticleSystem m_particleSystem;

	private LineRenderer m_lineRenderer;

	public UnityEvent SetColorEvent;

	private MeshRenderer meshRend;

	public ColorType colorType;

	private void Awake()
	{
		m_spriteRenderer = GetComponent<SpriteRenderer>();
		meshRend = GetComponent<MeshRenderer>();
		m_lineRenderer = GetComponent<LineRenderer>();
	}

	public void Set(PlayerSkin teamColor)
	{
		Color color = teamColor.color;
		if (colorType == ColorType.Background)
		{
			color = teamColor.backgroundColor;
		}
		if (colorType == ColorType.Particle)
		{
			color = teamColor.particleEffect;
		}
		if (colorType == ColorType.WinText)
		{
			color = teamColor.winText;
		}
		if ((bool)m_lineRenderer)
		{
			m_lineRenderer.startColor = color;
			m_lineRenderer.endColor = color;
		}
		else if ((bool)m_spriteRenderer)
		{
			m_spriteRenderer.color = color;
		}
		else if ((bool)meshRend)
		{
			meshRend.material.color = color;
		}
		else
		{
			m_particleSystem = GetComponent<ParticleSystem>();
			if ((bool)m_particleSystem)
			{
				ParticleSystem.MainModule main = m_particleSystem.main;
				main.startColor = color;
			}
		}
		SetColorEvent.Invoke();
	}

	public static void TeamColorThis(GameObject go, PlayerSkin teamColor)
	{
		if (!(teamColor == null))
		{
			SetTeamColor[] componentsInChildren = go.GetComponentsInChildren<SetTeamColor>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Set(teamColor);
			}
		}
	}
}
