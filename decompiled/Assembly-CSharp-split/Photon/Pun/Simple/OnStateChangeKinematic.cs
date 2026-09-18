using UnityEngine;

namespace Photon.Pun.Simple;

[DisallowMultipleComponent]
public class OnStateChangeKinematic : NetComponent, IOnStateChange, IApplyOrder, IAutoKinematic
{
	public KinematicSetting onDespawned = KinematicSetting.Kinematic;

	public KinematicSetting onAnchored = KinematicSetting.Kinematic;

	public KinematicSetting onMounted = KinematicSetting.NonKinematic;

	public KinematicSetting onTransit = KinematicSetting.NonKinematic;

	public KinematicSetting onDropped = KinematicSetting.NonKinematic;

	public KinematicSetting onVisible = KinematicSetting.Default;

	[Tooltip("Destroy this component if no Rigidbodies exist on this GameObject.")]
	public bool autoDestroy = true;

	private ObjState currentState;

	private Rigidbody rb;

	private Rigidbody2D rb2d;

	private bool kinematicDefault;

	private int interpolateDefault;

	public int ApplyOrder => 11;

	public bool AutoKinematicEnabled => true;

	public override void OnAwake()
	{
		base.OnAwake();
		rb = netObj.Rb;
		rb2d = netObj.Rb2D;
		if ((bool)rb)
		{
			kinematicDefault = rb.isKinematic;
			interpolateDefault = (int)rb.interpolation;
		}
		else if ((bool)rb2d)
		{
			kinematicDefault = rb2d.isKinematic;
			interpolateDefault = (int)rb2d.interpolation;
		}
		if (autoDestroy && !rb && !rb2d)
		{
			Object.Destroy(this);
		}
		SetOwnedKinematics(currentState);
	}

	public override void OnAuthorityChanged(bool isMine, bool controllerChanged)
	{
		base.OnAuthorityChanged(isMine, controllerChanged);
		if (isMine)
		{
			SetOwnedKinematics(currentState);
		}
		else
		{
			SetUnownedKinematics();
		}
	}

	public void OnStateChange(ObjState newState, ObjState previousState, Transform attachmentTransform, Mount attachTo = null, bool isReady = true)
	{
		if (base.IsMine)
		{
			SetOwnedKinematics(newState);
		}
	}

	protected virtual void SetUnownedKinematics()
	{
		if (base.RigidbodyType != RigidbodyType.None)
		{
			if (base.RigidbodyType == RigidbodyType.RB)
			{
				rb.isKinematic = true;
				rb.interpolation = RigidbodyInterpolation.None;
			}
			else
			{
				rb2d.isKinematic = true;
				rb2d.interpolation = RigidbodyInterpolation2D.None;
			}
		}
	}

	protected virtual void SetOwnedKinematics(ObjState state)
	{
		if (base.RigidbodyType == RigidbodyType.None)
		{
			return;
		}
		bool flag = ((state == ObjState.Despawned && onDespawned != KinematicSetting.Ignore) ? onDespawned : (((state & ObjState.Anchored) != ObjState.Despawned && onAnchored != KinematicSetting.Ignore) ? onAnchored : (((state & ObjState.Mounted) != ObjState.Despawned && onMounted != KinematicSetting.Ignore) ? onMounted : (((state & ObjState.Transit) != ObjState.Despawned && onTransit != KinematicSetting.Ignore) ? onTransit : (((state & ObjState.Dropped) != ObjState.Despawned && onDropped != KinematicSetting.Ignore) ? onDropped : (((state & ObjState.Visible) == 0 || onVisible == KinematicSetting.Ignore) ? KinematicSetting.Default : onVisible)))))) switch
		{
			KinematicSetting.Kinematic => true, 
			KinematicSetting.NonKinematic => false, 
			_ => kinematicDefault, 
		};
		if (base.RigidbodyType == RigidbodyType.RB)
		{
			rb.position = base.transform.position;
			if (flag)
			{
				rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
				rb.isKinematic = true;
			}
			else
			{
				rb.isKinematic = false;
			}
			rb.interpolation = ((state != ObjState.Despawned && (state & ObjState.Mounted) == 0) ? ((RigidbodyInterpolation)interpolateDefault) : RigidbodyInterpolation.None);
		}
		else
		{
			rb2d.position = base.transform.position;
			rb2d.isKinematic = flag;
			rb2d.simulated = !flag;
			rb2d.interpolation = ((state != ObjState.Despawned && (state & ObjState.Mounted) == 0) ? ((RigidbodyInterpolation2D)interpolateDefault) : RigidbodyInterpolation2D.None);
		}
		currentState = state;
	}
}
