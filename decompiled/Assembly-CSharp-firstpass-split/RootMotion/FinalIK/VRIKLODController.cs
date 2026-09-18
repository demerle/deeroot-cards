using UnityEngine;

namespace RootMotion.FinalIK;

public class VRIKLODController : MonoBehaviour
{
	public Renderer LODRenderer;

	public float LODDistance = 15f;

	public bool allowCulled = true;

	private VRIK ik;

	private void Start()
	{
		ik = GetComponent<VRIK>();
	}

	private void Update()
	{
		ik.solver.LOD = GetLODLevel();
	}

	private int GetLODLevel()
	{
		if (allowCulled)
		{
			if (LODRenderer == null)
			{
				return 0;
			}
			if (!LODRenderer.isVisible)
			{
				return 2;
			}
		}
		if ((ik.transform.position - Camera.main.transform.position).sqrMagnitude > LODDistance * LODDistance)
		{
			return 1;
		}
		return 0;
	}
}
