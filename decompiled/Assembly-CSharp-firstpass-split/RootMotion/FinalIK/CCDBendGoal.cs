using System;
using UnityEngine;

namespace RootMotion.FinalIK;

public class CCDBendGoal : MonoBehaviour
{
	public CCDIK ik;

	[Range(0f, 1f)]
	public float weight = 1f;

	private void Start()
	{
		IKSolverCCD solver = ik.solver;
		solver.OnPreUpdate = (IKSolver.UpdateDelegate)Delegate.Combine(solver.OnPreUpdate, new IKSolver.UpdateDelegate(BeforeIK));
	}

	private void BeforeIK()
	{
		if (!base.enabled)
		{
			return;
		}
		float num = ik.solver.IKPositionWeight * weight;
		if (!(num <= 0f))
		{
			Vector3 position = ik.solver.bones[0].transform.position;
			Quaternion quaternion = Quaternion.FromToRotation(ik.solver.bones[ik.solver.bones.Length - 1].transform.position - position, base.transform.position - position);
			if (num < 1f)
			{
				quaternion = Quaternion.Slerp(Quaternion.identity, quaternion, num);
			}
			ik.solver.bones[0].transform.rotation = quaternion * ik.solver.bones[0].transform.rotation;
		}
	}

	private void OnDestroy()
	{
		if (ik != null)
		{
			IKSolverCCD solver = ik.solver;
			solver.OnPreUpdate = (IKSolver.UpdateDelegate)Delegate.Remove(solver.OnPreUpdate, new IKSolver.UpdateDelegate(BeforeIK));
		}
	}
}
