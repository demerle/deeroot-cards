using System;
using UnityEngine;

namespace RootMotion.FinalIK;

public class VRIKRootController : MonoBehaviour
{
	private Transform pelvisTarget;

	private Transform leftFootTarget;

	private Transform rightFootTarget;

	private VRIK ik;

	public Vector3 pelvisTargetRight { get; private set; }

	private void Awake()
	{
		ik = GetComponent<VRIK>();
		IKSolverVR solver = ik.solver;
		solver.OnPreUpdate = (IKSolver.UpdateDelegate)Delegate.Combine(solver.OnPreUpdate, new IKSolver.UpdateDelegate(OnPreUpdate));
		Calibrate();
	}

	public void Calibrate()
	{
		if (ik == null)
		{
			Debug.LogError("No VRIK found on VRIKRootController's GameObject.", base.transform);
			return;
		}
		pelvisTarget = ik.solver.spine.pelvisTarget;
		leftFootTarget = ik.solver.leftLeg.target;
		rightFootTarget = ik.solver.rightLeg.target;
		if (pelvisTarget != null)
		{
			pelvisTargetRight = Quaternion.Inverse(pelvisTarget.rotation) * ik.references.root.right;
		}
	}

	public void Calibrate(VRIKCalibrator.CalibrationData data)
	{
		if (ik == null)
		{
			Debug.LogError("No VRIK found on VRIKRootController's GameObject.", base.transform);
			return;
		}
		pelvisTarget = ik.solver.spine.pelvisTarget;
		leftFootTarget = ik.solver.leftLeg.target;
		rightFootTarget = ik.solver.rightLeg.target;
		if (pelvisTarget != null)
		{
			pelvisTargetRight = data.pelvisTargetRight;
		}
	}

	private void OnPreUpdate()
	{
		if (base.enabled)
		{
			if (pelvisTarget != null)
			{
				ik.references.root.position = new Vector3(pelvisTarget.position.x, ik.references.root.position.y, pelvisTarget.position.z);
				Vector3 forward = Vector3.Cross(pelvisTarget.rotation * pelvisTargetRight, ik.references.root.up);
				forward.y = 0f;
				ik.references.root.rotation = Quaternion.LookRotation(forward);
				ik.references.pelvis.position = Vector3.Lerp(ik.references.pelvis.position, pelvisTarget.position, ik.solver.spine.pelvisPositionWeight);
				ik.references.pelvis.rotation = Quaternion.Slerp(ik.references.pelvis.rotation, pelvisTarget.rotation, ik.solver.spine.pelvisRotationWeight);
			}
			else if (leftFootTarget != null && rightFootTarget != null)
			{
				ik.references.root.position = Vector3.Lerp(leftFootTarget.position, rightFootTarget.position, 0.5f);
			}
		}
	}

	private void OnDestroy()
	{
		if (ik != null)
		{
			IKSolverVR solver = ik.solver;
			solver.OnPreUpdate = (IKSolver.UpdateDelegate)Delegate.Remove(solver.OnPreUpdate, new IKSolver.UpdateDelegate(OnPreUpdate));
		}
	}
}
