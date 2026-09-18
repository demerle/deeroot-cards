using Sirenix.OdinInspector;
using UnityEngine;

namespace Rounds;

[ExecuteInEditMode]
public class FakeParticle : MonoBehaviour
{
	[OnValueChanged("OnChangedValue", false)]
	public float m_ScaleFactor = 1f;

	private Material m_material;

	private SpriteRenderer m_renderer;

	private MaterialPropertyBlock m_propertyBlock;

	private void OnEnable()
	{
		m_renderer = GetComponent<SpriteRenderer>();
		m_propertyBlock = new MaterialPropertyBlock();
		if (m_material == null)
		{
			m_material = FakeParticleDB.GetParticleMaterial();
		}
		if (m_renderer == null)
		{
			Debug.LogError("No Sprite Renderer on " + base.gameObject.name + " and it's needed for a FakeParticle.", base.gameObject);
		}
		else
		{
			OnChangedValueInit();
		}
	}

	private void Update()
	{
		OnChangedValue();
	}

	private void OnDisable()
	{
		m_renderer.material = FakeParticleDB.GetDefaultMaterial();
	}

	private void OnChangedValue()
	{
		m_renderer.material = m_material;
		m_renderer.GetPropertyBlock(m_propertyBlock);
		m_propertyBlock.SetFloat("_ScaleFactor", m_ScaleFactor);
		m_propertyBlock.SetFloat("_Offset", Random.Range(0, 1000));
		m_renderer.SetPropertyBlock(m_propertyBlock);
	}

	private void OnChangedValueInit()
	{
		m_renderer.material = m_material;
		m_renderer.GetPropertyBlock(m_propertyBlock);
		m_propertyBlock.SetFloat("_ScaleFactor", m_ScaleFactor);
		m_propertyBlock.SetFloat("_Offset", Random.Range(0, 1000));
		m_renderer.SetPropertyBlock(m_propertyBlock);
	}
}
