using UnityEngine;
using UnityEngine.Playables;

namespace RootMotion;

public class HumanoidBaker : Baker
{
	[Tooltip("Should the hand IK curves be added to the animation? Disable this if the original hand positions are not important when using the clip on another character via Humanoid retargeting.")]
	public bool bakeHandIK = true;

	[Tooltip("Max keyframe reduction error for the Root.Q/T, LeftFoot IK and RightFoot IK channels. Having a larger error value for 'Key Reduction Error' and a smaller one for this enables you to optimize clip data size without the floating feet effect by enabling 'Foot IK' in the Animator.")]
	[Range(0f, 0.1f)]
	public float IKKeyReductionError;

	[Tooltip("Frame rate divider for the muscle curves. If you have 'Frame Rate' set to 30, and this value set to 3, the muscle curves will be baked at 10 fps. Only the Root Q/T and Hand and Foot IK curves will be baked at 30. This enables you to optimize clip data size without the floating feet effect by enabling 'Foot IK' in the Animator.")]
	[Range(1f, 9f)]
	public int muscleFrameRateDiv = 1;

	private BakerMuscle[] bakerMuscles;

	private BakerHumanoidQT rootQT;

	private BakerHumanoidQT leftFootQT;

	private BakerHumanoidQT rightFootQT;

	private BakerHumanoidQT leftHandQT;

	private BakerHumanoidQT rightHandQT;

	private float[] muscles = new float[0];

	private HumanPose pose;

	private HumanPoseHandler handler;

	private Vector3 bodyPosition;

	private Quaternion bodyRotation = Quaternion.identity;

	private int mN;

	private Quaternion lastBodyRotation = Quaternion.identity;

	private void Awake()
	{
		animator = GetComponent<Animator>();
		director = GetComponent<PlayableDirector>();
		if (mode == Mode.AnimationStates || mode == Mode.AnimationClips)
		{
			if (animator == null || !animator.isHuman)
			{
				Debug.LogError("HumanoidBaker GameObject does not have a Humanoid Animator component, can not bake.");
				base.enabled = false;
				return;
			}
			animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
		}
		else if (mode == Mode.PlayableDirector && director == null)
		{
			Debug.LogError("HumanoidBaker GameObject does not have a PlayableDirector component, can not bake.");
		}
		muscles = new float[HumanTrait.MuscleCount];
		bakerMuscles = new BakerMuscle[HumanTrait.MuscleCount];
		for (int i = 0; i < bakerMuscles.Length; i++)
		{
			bakerMuscles[i] = new BakerMuscle(i);
		}
		rootQT = new BakerHumanoidQT("Root");
		leftFootQT = new BakerHumanoidQT(animator.GetBoneTransform(HumanBodyBones.LeftFoot), AvatarIKGoal.LeftFoot, "LeftFoot");
		rightFootQT = new BakerHumanoidQT(animator.GetBoneTransform(HumanBodyBones.RightFoot), AvatarIKGoal.RightFoot, "RightFoot");
		leftHandQT = new BakerHumanoidQT(animator.GetBoneTransform(HumanBodyBones.LeftHand), AvatarIKGoal.LeftHand, "LeftHand");
		rightHandQT = new BakerHumanoidQT(animator.GetBoneTransform(HumanBodyBones.RightHand), AvatarIKGoal.RightHand, "RightHand");
		handler = new HumanPoseHandler(animator.avatar, animator.transform);
	}

	protected override Transform GetCharacterRoot()
	{
		return animator.transform;
	}

	protected override void OnStartBaking()
	{
		rootQT.Reset();
		leftFootQT.Reset();
		rightFootQT.Reset();
		leftHandQT.Reset();
		rightHandQT.Reset();
		for (int i = 0; i < bakerMuscles.Length; i++)
		{
			bakerMuscles[i].Reset();
		}
		mN = muscleFrameRateDiv;
		lastBodyRotation = Quaternion.identity;
	}

	protected override void OnSetLoopFrame(float time)
	{
		for (int i = 0; i < bakerMuscles.Length; i++)
		{
			bakerMuscles[i].SetLoopFrame(time);
		}
		rootQT.MoveLastKeyframes(time);
		leftFootQT.SetLoopFrame(time);
		rightFootQT.SetLoopFrame(time);
		leftHandQT.SetLoopFrame(time);
		rightHandQT.SetLoopFrame(time);
	}

	protected override void OnSetCurves(ref AnimationClip clip)
	{
		float time = bakerMuscles[0].curve.keys[bakerMuscles[0].curve.keys.Length - 1].time;
		float lengthMlp = ((mode != Mode.Realtime) ? (base.clipLength / time) : 1f);
		for (int i = 0; i < bakerMuscles.Length; i++)
		{
			bakerMuscles[i].SetCurves(ref clip, keyReductionError, lengthMlp);
		}
		rootQT.SetCurves(ref clip, IKKeyReductionError, lengthMlp);
		leftFootQT.SetCurves(ref clip, IKKeyReductionError, lengthMlp);
		rightFootQT.SetCurves(ref clip, IKKeyReductionError, lengthMlp);
		if (bakeHandIK)
		{
			leftHandQT.SetCurves(ref clip, IKKeyReductionError, lengthMlp);
			rightHandQT.SetCurves(ref clip, IKKeyReductionError, lengthMlp);
		}
	}

	protected override void OnSetKeyframes(float time, bool lastFrame)
	{
		mN++;
		bool flag = true;
		if (mN < muscleFrameRateDiv && !lastFrame)
		{
			flag = false;
		}
		if (mN >= muscleFrameRateDiv)
		{
			mN = 0;
		}
		UpdateHumanPose();
		if (flag)
		{
			for (int i = 0; i < bakerMuscles.Length; i++)
			{
				bakerMuscles[i].SetKeyframe(time, muscles);
			}
		}
		rootQT.SetKeyframes(time, bodyPosition, bodyRotation);
		Vector3 vector = bodyPosition * animator.humanScale;
		leftFootQT.SetIKKeyframes(time, animator.avatar, animator.transform, animator.humanScale, vector, bodyRotation);
		rightFootQT.SetIKKeyframes(time, animator.avatar, animator.transform, animator.humanScale, vector, bodyRotation);
		leftHandQT.SetIKKeyframes(time, animator.avatar, animator.transform, animator.humanScale, vector, bodyRotation);
		rightHandQT.SetIKKeyframes(time, animator.avatar, animator.transform, animator.humanScale, vector, bodyRotation);
	}

	private void UpdateHumanPose()
	{
		handler.GetHumanPose(ref pose);
		bodyPosition = pose.bodyPosition;
		bodyRotation = pose.bodyRotation;
		bodyRotation = BakerUtilities.EnsureQuaternionContinuity(lastBodyRotation, bodyRotation);
		lastBodyRotation = bodyRotation;
		for (int i = 0; i < pose.muscles.Length; i++)
		{
			muscles[i] = pose.muscles[i];
		}
	}
}
