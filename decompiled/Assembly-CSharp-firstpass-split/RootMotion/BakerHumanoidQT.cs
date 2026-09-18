using System;
using UnityEngine;

namespace RootMotion;

[Serializable]
public class BakerHumanoidQT
{
	private Transform transform;

	private string Qx;

	private string Qy;

	private string Qz;

	private string Qw;

	private string Tx;

	private string Ty;

	private string Tz;

	public AnimationCurve rotX;

	public AnimationCurve rotY;

	public AnimationCurve rotZ;

	public AnimationCurve rotW;

	public AnimationCurve posX;

	public AnimationCurve posY;

	public AnimationCurve posZ;

	private AvatarIKGoal goal;

	private Quaternion lastQ;

	private bool lastQSet;

	public BakerHumanoidQT(string name)
	{
		Qx = name + "Q.x";
		Qy = name + "Q.y";
		Qz = name + "Q.z";
		Qw = name + "Q.w";
		Tx = name + "T.x";
		Ty = name + "T.y";
		Tz = name + "T.z";
		Reset();
	}

	public BakerHumanoidQT(Transform transform, AvatarIKGoal goal, string name)
	{
		this.transform = transform;
		this.goal = goal;
		Qx = name + "Q.x";
		Qy = name + "Q.y";
		Qz = name + "Q.z";
		Qw = name + "Q.w";
		Tx = name + "T.x";
		Ty = name + "T.y";
		Tz = name + "T.z";
		Reset();
	}

	public void Reset()
	{
		rotX = new AnimationCurve();
		rotY = new AnimationCurve();
		rotZ = new AnimationCurve();
		rotW = new AnimationCurve();
		posX = new AnimationCurve();
		posY = new AnimationCurve();
		posZ = new AnimationCurve();
		lastQ = Quaternion.identity;
		lastQSet = false;
	}

	public void SetIKKeyframes(float time, Avatar avatar, Transform root, float humanScale, Vector3 bodyPosition, Quaternion bodyRotation)
	{
		Vector3 vector = transform.position;
		Quaternion quaternion = transform.rotation;
		if (root.parent != null)
		{
			vector = root.parent.InverseTransformPoint(vector);
			quaternion = Quaternion.Inverse(root.parent.rotation) * quaternion;
		}
		TQ iKGoalTQ = AvatarUtility.GetIKGoalTQ(avatar, humanScale, goal, new TQ(bodyPosition, bodyRotation), new TQ(vector, quaternion));
		Quaternion quaternion2 = iKGoalTQ.q;
		if (lastQSet)
		{
			quaternion2 = BakerUtilities.EnsureQuaternionContinuity(lastQ, iKGoalTQ.q);
		}
		lastQ = quaternion2;
		lastQSet = true;
		rotX.AddKey(time, quaternion2.x);
		rotY.AddKey(time, quaternion2.y);
		rotZ.AddKey(time, quaternion2.z);
		rotW.AddKey(time, quaternion2.w);
		Vector3 t = iKGoalTQ.t;
		posX.AddKey(time, t.x);
		posY.AddKey(time, t.y);
		posZ.AddKey(time, t.z);
	}

	public void SetKeyframes(float time, Vector3 pos, Quaternion rot)
	{
		rotX.AddKey(time, rot.x);
		rotY.AddKey(time, rot.y);
		rotZ.AddKey(time, rot.z);
		rotW.AddKey(time, rot.w);
		posX.AddKey(time, pos.x);
		posY.AddKey(time, pos.y);
		posZ.AddKey(time, pos.z);
	}

	public void MoveLastKeyframes(float time)
	{
		MoveLastKeyframe(time, rotX);
		MoveLastKeyframe(time, rotY);
		MoveLastKeyframe(time, rotZ);
		MoveLastKeyframe(time, rotW);
		MoveLastKeyframe(time, posX);
		MoveLastKeyframe(time, posY);
		MoveLastKeyframe(time, posZ);
	}

	public void SetLoopFrame(float time)
	{
		BakerUtilities.SetLoopFrame(time, rotX);
		BakerUtilities.SetLoopFrame(time, rotY);
		BakerUtilities.SetLoopFrame(time, rotZ);
		BakerUtilities.SetLoopFrame(time, rotW);
		BakerUtilities.SetLoopFrame(time, posX);
		BakerUtilities.SetLoopFrame(time, posY);
		BakerUtilities.SetLoopFrame(time, posZ);
	}

	private void MoveLastKeyframe(float time, AnimationCurve curve)
	{
		Keyframe[] keys = curve.keys;
		keys[^1].time = time;
		curve.keys = keys;
	}

	public void MultiplyLength(AnimationCurve curve, float mlp)
	{
		Keyframe[] keys = curve.keys;
		for (int i = 0; i < keys.Length; i++)
		{
			keys[i].time *= mlp;
		}
		curve.keys = keys;
	}

	public void SetCurves(ref AnimationClip clip, float maxError, float lengthMlp)
	{
		MultiplyLength(rotX, lengthMlp);
		MultiplyLength(rotY, lengthMlp);
		MultiplyLength(rotZ, lengthMlp);
		MultiplyLength(rotW, lengthMlp);
		MultiplyLength(posX, lengthMlp);
		MultiplyLength(posY, lengthMlp);
		MultiplyLength(posZ, lengthMlp);
		BakerUtilities.ReduceKeyframes(rotX, maxError);
		BakerUtilities.ReduceKeyframes(rotY, maxError);
		BakerUtilities.ReduceKeyframes(rotZ, maxError);
		BakerUtilities.ReduceKeyframes(rotW, maxError);
		BakerUtilities.ReduceKeyframes(posX, maxError);
		BakerUtilities.ReduceKeyframes(posY, maxError);
		BakerUtilities.ReduceKeyframes(posZ, maxError);
		BakerUtilities.SetTangentMode(rotX);
		BakerUtilities.SetTangentMode(rotY);
		BakerUtilities.SetTangentMode(rotZ);
		BakerUtilities.SetTangentMode(rotW);
		clip.SetCurve(string.Empty, typeof(Animator), Qx, rotX);
		clip.SetCurve(string.Empty, typeof(Animator), Qy, rotY);
		clip.SetCurve(string.Empty, typeof(Animator), Qz, rotZ);
		clip.SetCurve(string.Empty, typeof(Animator), Qw, rotW);
		clip.SetCurve(string.Empty, typeof(Animator), Tx, posX);
		clip.SetCurve(string.Empty, typeof(Animator), Ty, posY);
		clip.SetCurve(string.Empty, typeof(Animator), Tz, posZ);
	}
}
