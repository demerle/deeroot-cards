using UnityEngine;

[CreateAssetMenu(fileName = "FakeParticleDB", menuName = "FakeParticleDB", order = 999999999)]
public class FakeParticleDB : ScriptableObject
{
	public Material m_DefaultMaterial;

	public Material m_ParticleMaterial;

	private static FakeParticleDB _inst;

	private static FakeParticleDB instance
	{
		get
		{
			if (_inst == null)
			{
				_inst = Resources.Load("FakeParticleDB") as FakeParticleDB;
			}
			return _inst;
		}
	}

	public static Material GetParticleMaterial()
	{
		return instance.m_ParticleMaterial;
	}

	public static Material GetDefaultMaterial()
	{
		return instance.m_DefaultMaterial;
	}
}
