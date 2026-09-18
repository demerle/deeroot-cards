using System;
using System.Reflection;
using UnityEngine;

namespace RootMotion;

public class AvatarUtility
{
	public static Quaternion GetPostRotation(Avatar avatar, AvatarIKGoal avatarIKGoal)
	{
		int num = (int)HumanIDFromAvatarIKGoal(avatarIKGoal);
		if (num == 55)
		{
			throw new InvalidOperationException("Invalid human id.");
		}
		MethodInfo method = typeof(Avatar).GetMethod("GetPostRotation", BindingFlags.Instance | BindingFlags.NonPublic);
		if (method == null)
		{
			throw new InvalidOperationException("Cannot find GetPostRotation method.");
		}
		return (Quaternion)method.Invoke(avatar, new object[1] { num });
	}

	public static TQ GetIKGoalTQ(Avatar avatar, float humanScale, AvatarIKGoal avatarIKGoal, TQ bodyPositionRotation, TQ boneTQ)
	{
		int num = (int)HumanIDFromAvatarIKGoal(avatarIKGoal);
		if (num == 55)
		{
			throw new InvalidOperationException("Invalid human id.");
		}
		MethodInfo method = typeof(Avatar).GetMethod("GetAxisLength", BindingFlags.Instance | BindingFlags.NonPublic);
		if (method == null)
		{
			throw new InvalidOperationException("Cannot find GetAxisLength method.");
		}
		MethodInfo method2 = typeof(Avatar).GetMethod("GetPostRotation", BindingFlags.Instance | BindingFlags.NonPublic);
		if (method2 == null)
		{
			throw new InvalidOperationException("Cannot find GetPostRotation method.");
		}
		Quaternion quaternion = (Quaternion)method2.Invoke(avatar, new object[1] { num });
		TQ tQ = new TQ(boneTQ.t, boneTQ.q * quaternion);
		if (avatarIKGoal == AvatarIKGoal.LeftFoot || avatarIKGoal == AvatarIKGoal.RightFoot)
		{
			float x = (float)method.Invoke(avatar, new object[1] { num });
			Vector3 vector = new Vector3(x, 0f, 0f);
			tQ.t += tQ.q * vector;
		}
		Quaternion quaternion2 = Quaternion.Inverse(bodyPositionRotation.q);
		tQ.t = quaternion2 * (tQ.t - bodyPositionRotation.t);
		tQ.q = quaternion2 * tQ.q;
		tQ.t /= humanScale;
		tQ.q = Quaternion.LookRotation(tQ.q * Vector3.forward, tQ.q * Vector3.up);
		return tQ;
	}

	public static HumanBodyBones HumanIDFromAvatarIKGoal(AvatarIKGoal avatarIKGoal)
	{
		HumanBodyBones result = HumanBodyBones.LastBone;
		switch (avatarIKGoal)
		{
		case AvatarIKGoal.LeftFoot:
			result = HumanBodyBones.LeftFoot;
			break;
		case AvatarIKGoal.RightFoot:
			result = HumanBodyBones.RightFoot;
			break;
		case AvatarIKGoal.LeftHand:
			result = HumanBodyBones.LeftHand;
			break;
		case AvatarIKGoal.RightHand:
			result = HumanBodyBones.RightHand;
			break;
		}
		return result;
	}
}
