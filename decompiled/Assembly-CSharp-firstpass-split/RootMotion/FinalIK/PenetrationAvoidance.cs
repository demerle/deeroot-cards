using System;
using UnityEngine;

namespace RootMotion.FinalIK;

public class PenetrationAvoidance : OffsetModifier
{
	[Serializable]
	public class Avoider
	{
		[Serializable]
		public class EffectorLink
		{
			[Tooltip("Effector to apply the offset to.")]
			public FullBodyBipedEffector effector;

			[Tooltip("Multiplier of the offset value, can be negative.")]
			public float weight;
		}

		[Tooltip("Bones to start the raycast from. Multiple raycasts can be used by assigning more than 1 bone.")]
		public Transform[] raycastFrom;

		[Tooltip("The Transform to raycast towards. Usually the body part that you want to keep from penetrating.")]
		public Transform raycastTo;

		[Tooltip("If 0, will use simple raycasting, if > 0, will use sphere casting (better, but slower).")]
		[Range(0f, 1f)]
		public float raycastRadius;

		[Tooltip("Linking this to FBBIK effectors.")]
		public EffectorLink[] effectors;

		[Tooltip("The time of smooth interpolation of the offset value to avoid penetration.")]
		public float smoothTimeIn = 0.1f;

		[Tooltip("The time of smooth interpolation of the offset value blending out of penetration avoidance.")]
		public float smoothTimeOut = 0.3f;

		[Tooltip("Layers to keep penetrating from.")]
		public LayerMask layers;

		private Vector3 offset;

		private Vector3 offsetTarget;

		private Vector3 offsetV;

		public void Solve(IKSolverFullBodyBiped solver, float weight)
		{
			offsetTarget = GetOffsetTarget(solver);
			float smoothTime = ((offsetTarget.sqrMagnitude > offset.sqrMagnitude) ? smoothTimeIn : smoothTimeOut);
			offset = Vector3.SmoothDamp(offset, offsetTarget, ref offsetV, smoothTime);
			EffectorLink[] array = effectors;
			foreach (EffectorLink effectorLink in array)
			{
				solver.GetEffector(effectorLink.effector).positionOffset += offset * weight * effectorLink.weight;
			}
		}

		private Vector3 GetOffsetTarget(IKSolverFullBodyBiped solver)
		{
			Vector3 zero = Vector3.zero;
			Transform[] array = raycastFrom;
			foreach (Transform transform in array)
			{
				zero += Raycast(transform.position, raycastTo.position + zero);
			}
			return zero;
		}

		private Vector3 Raycast(Vector3 from, Vector3 to)
		{
			Vector3 direction = to - from;
			float magnitude = direction.magnitude;
			RaycastHit hitInfo;
			if (raycastRadius <= 0f)
			{
				Physics.Raycast(from, direction, out hitInfo, magnitude, layers);
			}
			else
			{
				Physics.SphereCast(from, raycastRadius, direction, out hitInfo, magnitude, layers);
			}
			if (hitInfo.collider == null)
			{
				return Vector3.zero;
			}
			return Vector3.Project(-direction.normalized * (magnitude - hitInfo.distance), hitInfo.normal);
		}
	}

	[Tooltip("Definitions of penetration avoidances.")]
	public Avoider[] avoiders;

	protected override void OnModifyOffset()
	{
		Avoider[] array = avoiders;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Solve(ik.solver, weight);
		}
	}
}
