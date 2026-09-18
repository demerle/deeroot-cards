using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;

namespace RootMotion.FinalIK;

[RequireComponent(typeof(Animator))]
[AddComponentMenu("Scripts/RootMotion.FinalIK/IK Jobs/CCD IKJ")]
public class CCDIKJ : MonoBehaviour
{
	[Tooltip("The target Transform of this solver.")]
	public Transform target;

	[Tooltip("The master weight of this solver.")]
	[Range(0f, 1f)]
	public float weight = 1f;

	[Tooltip("Minimum offset from last reached position. Will stop solving if offset is less than tolerance.If tolerance is zero, will iterate until maxIterations.")]
	public float tolerance;

	[Tooltip("Max solver iterations per frame. If target position offset is less than 'Tolerance', will stop solving.")]
	public int maxIterations = 4;

	[Tooltip("If true, rotation limits (if existing) will be applied on each iteration. Only RotationLimitAngle and RotationLimitHinge can be used with this solver.")]
	public bool useRotationLimits = true;

	[Tooltip("Useful for 2D games. If true, will solve only in the XY plane.")]
	public bool XY;

	[Tooltip("The list of bones used by the solver. Must be assigned in order of hierarchy. All bones must be in the same hierarchy branch.")]
	public Transform[] bones = new Transform[0];

	private Animator animator;

	private PlayableGraph graph;

	private AnimationScriptPlayable IKPlayable;

	private CCDIKJob job;

	public bool initiated { get; private set; }

	private void OnEnable()
	{
		if (bones.Length >= 3)
		{
			animator = GetComponent<Animator>();
			if (target == null)
			{
				target = new GameObject("CCDIKJ Target (" + base.name + ")").transform;
				target.position = bones[bones.Length - 1].position;
				target.rotation = bones[bones.Length - 1].rotation;
			}
			graph = PlayableGraph.Create("CCDIKJ");
			AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "ouput", GetComponent<Animator>());
			job = default(CCDIKJob);
			job.Setup(GetComponent<Animator>(), bones, target);
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
			if (bones.Length < 3)
			{
				Debug.LogError("CCDIKJ needs at least 3 bones to run.", base.transform);
				base.enabled = false;
			}
			else
			{
				OnEnable();
			}
		}
		else if (initiated && target == null)
		{
			Debug.LogError("CCDIKJ 'Target' has gone missing. Destroying CCDIKJ.", base.transform);
			Object.Destroy(this);
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
