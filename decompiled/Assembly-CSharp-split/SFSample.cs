using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class SFSample : MonoBehaviour
{
	private Material _material;

	public Vector2 _samplePosition = Vector2.zero;

	public bool _lineSample;

	public Vector2 samplePosition
	{
		get
		{
			return _samplePosition;
		}
		set
		{
			_samplePosition = value;
			if ((bool)_material)
			{
				_material.SetVector("_SamplePosition", _samplePosition);
			}
		}
	}

	public bool lineSample
	{
		get
		{
			return _lineSample;
		}
		set
		{
			_lineSample = value;
			if ((bool)_material)
			{
				if (value)
				{
					_material.EnableKeyword("LINESAMPLE_ON");
					_material.DisableKeyword("FIXEDSAMPLEPOINT_ON");
				}
				else
				{
					_material.DisableKeyword("LINESAMPLE_ON");
					_material.EnableKeyword("FIXEDSAMPLEPOINT_ON");
				}
			}
		}
	}

	private void Start()
	{
		Renderer component = GetComponent<Renderer>();
		Material sharedMaterial = component.sharedMaterial;
		if (sharedMaterial == null || sharedMaterial.shader.name != "Sprites/SFSoftShadow")
		{
			Debug.LogError("SFSample requires the attached renderer to be using the Sprites/SFSoftShadow shader.");
			return;
		}
		_material = new Material(sharedMaterial);
		component.material = _material;
		_material.SetFloat("_SoftHardMix", sharedMaterial.GetFloat("_SoftHardMix"));
		samplePosition = _samplePosition;
		lineSample = _lineSample;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.DrawIcon(base.transform.TransformPoint(_samplePosition), "SFDotGizmo.psd");
	}
}
