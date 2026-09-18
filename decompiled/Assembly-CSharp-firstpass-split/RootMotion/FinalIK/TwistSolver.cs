using System;
using UnityEngine;

namespace RootMotion.FinalIK;

[Serializable]
public class TwistSolver
{
	[Tooltip("The transform that this solver operates on.")]
	public Transform transform;

	[Tooltip("If this is the forearm roll bone, the parent should be the forearm bone. If null, will be found automatically.")]
	public Transform parent;

	[Tooltip("If this is the forearm roll bone, the child should be the hand bone. If null, will attempt to find automatically. Assign the hand manually if the hand bone is not a child of the roll bone.")]
	public Transform[] children = new Transform[0];

	[Tooltip("The weight of relaxing the twist of this Transform")]
	[Range(0f, 1f)]
	public float weight = 1f;

	[Tooltip("If 0.5, this Transform will be twisted half way from parent to child. If 1, the twist angle will be locked to the child and will rotate with along with it.")]
	[Range(0f, 1f)]
	public float parentChildCrossfade = 0.5f;

	[Tooltip("Rotation offset around the twist axis.")]
	[Range(-180f, 180f)]
	public float twistAngleOffset;

	private Vector3 twistAxis = Vector3.right;

	private Vector3 axis = Vector3.forward;

	private Vector3 axisRelativeToParentDefault;

	private Vector3 axisRelativeToChildDefault;

	private Quaternion[] childRotations;

	private bool inititated;

	public TwistSolver()
	{
		weight = 1f;
		parentChildCrossfade = 0.5f;
	}

	public void Initiate()
	{
		if (transform == null)
		{
			Debug.LogError("TwistRelaxer solver has unassigned Transform. TwistRelaxer.cs was restructured for FIK v2.0 to support multiple relaxers on the same body part and TwistRelaxer components need to be set up again, sorry for the inconvenience!", transform);
			return;
		}
		if (parent == null)
		{
			parent = transform.parent;
		}
		if (children.Length == 0)
		{
			if (transform.childCount == 0)
			{
				Transform[] componentsInChildren = parent.GetComponentsInChildren<Transform>();
				for (int i = 1; i < componentsInChildren.Length; i++)
				{
					if (componentsInChildren[i] != transform)
					{
						componentsInChildren = new Transform[1] { componentsInChildren[i] };
						break;
					}
				}
			}
			else
			{
				children = new Transform[1] { transform.GetChild(0) };
			}
		}
		if (children.Length == 0 || children[0] == null)
		{
			Debug.LogError("TwistRelaxer has no children assigned.", transform);
			return;
		}
		twistAxis = transform.InverseTransformDirection(children[0].position - transform.position);
		axis = new Vector3(twistAxis.y, twistAxis.z, twistAxis.x);
		Vector3 vector = transform.rotation * axis;
		axisRelativeToParentDefault = Quaternion.Inverse(parent.rotation) * vector;
		axisRelativeToChildDefault = Quaternion.Inverse(children[0].rotation) * vector;
		childRotations = new Quaternion[children.Length];
		inititated = true;
	}

	public void Relax()
	{
		if (inititated && !(weight <= 0f))
		{
			Quaternion rotation = transform.rotation;
			Quaternion quaternion = Quaternion.AngleAxis(twistAngleOffset, rotation * twistAxis);
			rotation = quaternion * rotation;
			Vector3 a = quaternion * parent.rotation * axisRelativeToParentDefault;
			Vector3 b = quaternion * children[0].rotation * axisRelativeToChildDefault;
			Vector3 vector = Vector3.Slerp(a, b, parentChildCrossfade);
			vector = Quaternion.Inverse(Quaternion.LookRotation(rotation * axis, rotation * twistAxis)) * vector;
			float num = Mathf.Atan2(vector.x, vector.z) * 57.29578f;
			for (int i = 0; i < children.Length; i++)
			{
				childRotations[i] = children[i].rotation;
			}
			transform.rotation = Quaternion.AngleAxis(num * weight, rotation * twistAxis) * rotation;
			for (int j = 0; j < children.Length; j++)
			{
				children[j].rotation = childRotations[j];
			}
		}
	}
}
