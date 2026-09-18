using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;

namespace RootMotion.FinalIK;

[RequireComponent(typeof(Animator))]
[AddComponentMenu("Scripts/RootMotion.FinalIK/IK Jobs/Aim IKJ")]
public class AimIKJ : MonoBehaviour
{
	[Tooltip("The target Transform of this solver.")]
	public Transform target;

	[Tooltip("Optional secondary target for another axis of the 'Aim Transform'. Needs 'Pole Weight' to be greater than 0 to have any effect.")]
	public Transform poleTarget;

	[Tooltip("The transform that you want to be aimed at the target. Needs to be a lineal descendant of the bone hierarchy. For example, if you wish to aim a gun, it should be the gun, one of it's children or the hand bone.")]
	public Transform aimTransform;

	[Space(5f)]
	[Tooltip("The X value of the local axis of the 'Aim Transform' that you want to be aimed at IKPosition.")]
	public float axisX;

	[Tooltip("The Y value of the local axis of the 'Aim Transform' that you want to be aimed at IKPosition.")]
	public float axisY;

	[Tooltip("The Z value of the local axis of the 'Aim Transform' that you want to be aimed at IKPosition.")]
	public float axisZ = 1f;

	[Space(5f)]
	[Tooltip("The X value of the local axis of the 'Aim Transform' that you want oriented towards the 'Pole Target'.")]
	public float poleAxisX;

	[Tooltip("The Y value of the local axis of the 'Aim Transform' that you want oriented towards the 'Pole Target'.")]
	public float poleAxisY = 1f;

	[Tooltip("The Z value of the local axis of the 'Aim Transform' that you want oriented towards the 'Pole Target'.")]
	public float poleAxisZ;

	[Space(5f)]
	[Tooltip("The master weight of this solver.")]
	[Range(0f, 1f)]
	public float weight = 1f;

	[Tooltip("The weight of the 'Pole Target'")]
	[Range(0f, 1f)]
	public float poleWeight;

	[Tooltip("Minimum angular offset from last reached angle. Will stop solving if offset is less than tolerance.If tolerance is zero, will iterate until maxIterations.")]
	public float tolerance;

	[Tooltip("Max solver iterations per frame. If target position offset is less than 'Tolerance', will stop solving.")]
	public int maxIterations = 4;

	[Tooltip("Clamping rotation of the solver. 0 is free rotation, 1 is completely clamped to animated rotation.")]
	[Range(0f, 1f)]
	public float clampWeight = 0.1f;

	[Tooltip("Number of sine smoothing iterations applied on clamping to make the clamping point smoother.")]
	[Range(0f, 2f)]
	public int clampSmoothing = 2;

	[Tooltip("If true, rotation limits (if existing) will be applied on each iteration. Only RotationLimitAngle and RotationLimitHinge can be used with this solver.")]
	public bool useRotationLimits = true;

	[Tooltip("Useful for 2D games. If true, will solve only in the XY plane.")]
	public bool XY;

	[Space(5f)]
	[Tooltip("The list of bones used by the solver. Must be assigned in order of hierarchy. All bones must be in the same hierarchy branch.")]
	public Transform[] bones = new Transform[0];

	private Animator animator;

	private PlayableGraph graph;

	private AnimationScriptPlayable IKPlayable;

	private AimIKJob job;

	public bool initiated { get; private set; }

	public Vector3 axis
	{
		get
		{
			return new Vector3(axisX, axisY, axisZ);
		}
		set
		{
			axisX = value.x;
			axisY = value.y;
			axisZ = value.z;
		}
	}

	public Vector3 poleAxis
	{
		get
		{
			return new Vector3(poleAxisX, poleAxisY, poleAxisZ);
		}
		set
		{
			poleAxisX = value.x;
			poleAxisY = value.y;
			poleAxisZ = value.z;
		}
	}

	private void OnEnable()
	{
		if (bones.Length >= 1 && !(axis == Vector3.zero) && (!(poleAxis == Vector3.zero) || !(poleWeight > 0f)))
		{
			animator = GetComponent<Animator>();
			if (aimTransform == null)
			{
				aimTransform = bones[bones.Length - 1];
			}
			if (target == null)
			{
				target = new GameObject("AimIKJ Target (" + base.name + ")").transform;
				target.position = bones[bones.Length - 1].position + aimTransform.rotation * axis * 3f;
			}
			if (poleTarget == null)
			{
				poleTarget = new GameObject("AimIKJ Pole Target (" + base.name + ")").transform;
				poleTarget.position = bones[bones.Length - 1].position + aimTransform.rotation * poleAxis * 3f;
			}
			graph = PlayableGraph.Create("AimIKJ");
			AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "ouput", GetComponent<Animator>());
			job = default(AimIKJob);
			job.Setup(GetComponent<Animator>(), bones, target, poleTarget, aimTransform);
			IKPlayable = AnimationScriptPlayable.Create(graph, job);
			PlayableExtensions.AddInput(sourcePlayable: AnimatorControllerPlayable.Create(graph, animator.runtimeAnimatorController), playable: IKPlayable, sourceOutputIndex: 0, weight: 1f);
			output.SetSourcePlayable(IKPlayable);
			graph.Play();
			initiated = true;
		}
	}

	private void Update()
	{
		if (!initiated)
		{
			if (bones.Length < 1)
			{
				Debug.LogError("AimIKJ needs at least 1 bone to run.", base.transform);
				base.enabled = false;
			}
			else if (new Vector3(axisX, axisY, axisZ) == Vector3.zero)
			{
				Debug.Log("AimIKJ 'Axis' must not be zero.", base.transform);
			}
			else if (new Vector3(poleAxisX, poleAxisY, poleAxisZ) == Vector3.zero && poleWeight > 0f)
			{
				Debug.Log("AimIKJ 'Pole Axis' must not be zero when 'Pole Weight' is greater than 0.", base.transform);
			}
			else
			{
				OnEnable();
			}
		}
		else if (initiated)
		{
			if (target == null)
			{
				Debug.LogError("AimIKJ 'Target' has gone missing, destroying AimIKJ.", base.transform);
				Object.Destroy(this);
			}
			else if (poleTarget == null)
			{
				Debug.LogError("AimIKJ 'Pole Target' has gone missing, destroying AimIKJ.", base.transform);
				Object.Destroy(this);
			}
			else if (aimTransform == null)
			{
				Debug.LogError("AimIKJ 'Aim transform' has gone missing, destroying AimIKJ.", base.transform);
				Object.Destroy(this);
			}
		}
	}

	private void OnDisable()
	{
		if (initiated)
		{
			job.Dispose();
			graph.Destroy();
			Object.Destroy(target);
		}
	}
}
