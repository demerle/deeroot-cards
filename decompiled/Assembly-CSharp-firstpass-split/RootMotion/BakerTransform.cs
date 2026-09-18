using System;
using UnityEngine;

namespace RootMotion;

[Serializable]
public class BakerTransform
{
	public Transform transform;

	public AnimationCurve posX;

	public AnimationCurve posY;

	public AnimationCurve posZ;

	public AnimationCurve rotX;

	public AnimationCurve rotY;

	public AnimationCurve rotZ;

	public AnimationCurve rotW;

	private string relativePath;

	private bool recordPosition;

	private Vector3 relativePosition;

	private bool isRootNode;

	private Quaternion relativeRotation;

	public BakerTransform(Transform transform, Transform root, bool recordPosition, bool isRootNode)
	{
		this.transform = transform;
		this.recordPosition = recordPosition || isRootNode;
		this.isRootNode = isRootNode;
		relativePath = string.Empty;
		Reset();
	}

	public void SetRelativeSpace(Vector3 position, Quaternion rotation)
	{
		relativePosition = position;
		relativeRotation = rotation;
	}

	public void SetCurves(ref AnimationClip clip)
	{
		if (recordPosition)
		{
			clip.SetCurve(relativePath, typeof(Transform), "localPosition.x", posX);
			clip.SetCurve(relativePath, typeof(Transform), "localPosition.y", posY);
			clip.SetCurve(relativePath, typeof(Transform), "localPosition.z", posZ);
		}
		clip.SetCurve(relativePath, typeof(Transform), "localRotation.x", rotX);
		clip.SetCurve(relativePath, typeof(Transform), "localRotation.y", rotY);
		clip.SetCurve(relativePath, typeof(Transform), "localRotation.z", rotZ);
		clip.SetCurve(relativePath, typeof(Transform), "localRotation.w", rotW);
		if (isRootNode)
		{
			AddRootMotionCurves(ref clip);
		}
		clip.EnsureQuaternionContinuity();
	}

	private void AddRootMotionCurves(ref AnimationClip clip)
	{
		if (recordPosition)
		{
			clip.SetCurve("", typeof(Animator), "MotionT.x", posX);
			clip.SetCurve("", typeof(Animator), "MotionT.y", posY);
			clip.SetCurve("", typeof(Animator), "MotionT.z", posZ);
		}
		clip.SetCurve("", typeof(Animator), "MotionQ.x", rotX);
		clip.SetCurve("", typeof(Animator), "MotionQ.y", rotY);
		clip.SetCurve("", typeof(Animator), "MotionQ.z", rotZ);
		clip.SetCurve("", typeof(Animator), "MotionQ.w", rotW);
	}

	public void Reset()
	{
		posX = new AnimationCurve();
		posY = new AnimationCurve();
		posZ = new AnimationCurve();
		rotX = new AnimationCurve();
		rotY = new AnimationCurve();
		rotZ = new AnimationCurve();
		rotW = new AnimationCurve();
	}

	public void ReduceKeyframes(float maxError)
	{
		BakerUtilities.ReduceKeyframes(rotX, maxError);
		BakerUtilities.ReduceKeyframes(rotY, maxError);
		BakerUtilities.ReduceKeyframes(rotZ, maxError);
		BakerUtilities.ReduceKeyframes(rotW, maxError);
		BakerUtilities.ReduceKeyframes(posX, maxError);
		BakerUtilities.ReduceKeyframes(posY, maxError);
		BakerUtilities.ReduceKeyframes(posZ, maxError);
	}

	public void SetKeyframes(float time)
	{
		if (recordPosition)
		{
			Vector3 vector = transform.localPosition;
			if (isRootNode)
			{
				vector = transform.position - relativePosition;
			}
			posX.AddKey(time, vector.x);
			posY.AddKey(time, vector.y);
			posZ.AddKey(time, vector.z);
		}
		Quaternion quaternion = transform.localRotation;
		if (isRootNode)
		{
			quaternion = Quaternion.Inverse(relativeRotation) * transform.rotation;
		}
		rotX.AddKey(time, quaternion.x);
		rotY.AddKey(time, quaternion.y);
		rotZ.AddKey(time, quaternion.z);
		rotW.AddKey(time, quaternion.w);
	}

	public void AddLoopFrame(float time)
	{
		if (recordPosition && !isRootNode)
		{
			posX.AddKey(time, posX.keys[0].value);
			posY.AddKey(time, posY.keys[0].value);
			posZ.AddKey(time, posZ.keys[0].value);
		}
		rotX.AddKey(time, rotX.keys[0].value);
		rotY.AddKey(time, rotY.keys[0].value);
		rotZ.AddKey(time, rotZ.keys[0].value);
		rotW.AddKey(time, rotW.keys[0].value);
	}
}
