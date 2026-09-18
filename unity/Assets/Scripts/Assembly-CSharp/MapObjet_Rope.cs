using System;
using System.Collections;
using Sonigon;
using UnityEngine;

public class MapObjet_Rope : MonoBehaviour
{
	public enum JointType
	{
		spring = 0,
		Distance = 1
	}

	[Header("Sound")]
	public bool soundRopePlay;

	public SoundEvent soundRopeLoop;

	private SoundParameterIntensity soundParameterIntensity = new SoundParameterIntensity(0f, UpdateMode.Continuous);

	private bool soundIsPlaying;

	private bool soundInitialized;

	private float soundRopeLengthCurrent;

	private float soundRopeLengthLast;

	private float soundRopeLengthVelocity;

	[Header("Settings")]
	public JointType jointType;

	private Map map;

	private LineRenderer lineRenderer;

	private AnchoredJoint2D joint;

	private Vector3 lastPos;

	private Vector3 vel1;

	private Vector3 vel2;

	private Vector3 postSnapPos1;

	private Vector3 postSnapPos2;

	private float sinceBreak;

	private void Start()
	{
		map = GetComponentInParent<Map>();
		map.hasRope = true;
		lineRenderer = GetComponent<LineRenderer>();
		Map obj = map;
		obj.mapIsReadyAction = (Action)Delegate.Combine(obj.mapIsReadyAction, new Action(Go));
		Map obj2 = map;
		obj2.mapMovingOutAction = (Action)Delegate.Combine(obj2.mapMovingOutAction, new Action(Leave));
	}

	private void Leave()
	{
		if ((bool)joint)
		{
			UnityEngine.Object.Destroy(joint);
		}
	}

	public void Go()
	{
		StartCoroutine(IGo());
	}

	private IEnumerator IGo()
	{
		yield return new WaitForSeconds(0f);
		Rigidbody2D rigidbody2D = null;
		Rigidbody2D rigidbody2D2 = null;
		for (int i = 0; i < map.allRigs.Length; i++)
		{
			Collider2D component = map.allRigs[i].GetComponent<Collider2D>();
			if ((bool)component)
			{
				if (component.OverlapPoint(base.transform.position))
				{
					rigidbody2D = map.allRigs[i];
				}
				if (component.OverlapPoint(base.transform.GetChild(0).position))
				{
					rigidbody2D2 = map.allRigs[i];
				}
			}
		}
		if ((bool)rigidbody2D)
		{
			AddJoint(rigidbody2D);
			if ((bool)rigidbody2D2)
			{
				joint.connectedBody = rigidbody2D2;
				joint.anchor = rigidbody2D.transform.InverseTransformPoint(base.transform.position);
				joint.connectedAnchor = rigidbody2D2.transform.InverseTransformPoint(base.transform.GetChild(0).position);
			}
			else
			{
				joint.anchor = rigidbody2D.transform.InverseTransformPoint(base.transform.position);
				joint.connectedAnchor = base.transform.GetChild(0).position;
			}
			joint.enableCollision = true;
		}
		else if ((bool)rigidbody2D2)
		{
			AddJoint(rigidbody2D2);
			joint.anchor = rigidbody2D2.transform.InverseTransformPoint(base.transform.GetChild(0).position);
			joint.connectedAnchor = base.transform.position;
			joint.enableCollision = true;
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void AddJoint(Rigidbody2D target)
	{
		switch (jointType)
		{
		case JointType.spring:
			joint = target.gameObject.AddComponent<SpringJoint2D>();
			break;
		case JointType.Distance:
			joint = target.gameObject.AddComponent<DistanceJoint2D>();
			break;
		}
	}

	private void OnDrawGizmos()
	{
		if (!joint)
		{
			if (!lineRenderer)
			{
				lineRenderer = GetComponent<LineRenderer>();
			}
			lineRenderer.SetPosition(0, base.transform.position);
			lineRenderer.SetPosition(1, base.transform.GetChild(0).position);
			if (Event.current.shift)
			{
				base.transform.GetChild(0).position = lastPos;
			}
			lastPos = base.transform.GetChild(0).position;
		}
	}

	private void OnDestroy()
	{
		soundIsPlaying = false;
		soundInitialized = false;
		SoundManager.Instance.StopAtPosition(soundRopeLoop, base.transform);
	}

	private void OnDisable()
	{
		soundIsPlaying = false;
		soundInitialized = false;
		SoundManager.Instance.StopAtPosition(soundRopeLoop, base.transform);
	}

	private void Update()
	{
		if ((bool)joint)
		{
			if ((bool)joint.attachedRigidbody && !joint.attachedRigidbody.gameObject.activeSelf)
			{
				postSnapPos1 = lineRenderer.GetPosition(0);
			}
			if ((bool)joint.connectedBody && !joint.connectedBody.gameObject.activeSelf)
			{
				postSnapPos2 = lineRenderer.GetPosition(1);
			}
			if (((bool)joint.attachedRigidbody && !joint.attachedRigidbody.gameObject.activeSelf) || ((bool)joint.connectedBody && !joint.connectedBody.gameObject.activeSelf))
			{
				sinceBreak += Time.deltaTime;
				if (Vector2.Distance(lineRenderer.GetPosition(0), lineRenderer.GetPosition(1)) < 0.2f)
				{
					if (soundIsPlaying)
					{
						SoundManager.Instance.StopAtPosition(soundRopeLoop, base.transform);
					}
					lineRenderer.enabled = false;
					return;
				}
			}
			if ((bool)joint.attachedRigidbody && joint.attachedRigidbody.gameObject.activeSelf)
			{
				vel1 = joint.attachedRigidbody.velocity * 1.5f;
			}
			if ((bool)joint.attachedRigidbody && !joint.attachedRigidbody.gameObject.activeSelf)
			{
				vel1 = FRILerp.Lerp(vel1, (lineRenderer.GetPosition(1) - lineRenderer.GetPosition(0)) * 15f * Mathf.Clamp(sinceBreak * 2f, 0f, 1f), 10f);
				postSnapPos1 += vel1 * Time.deltaTime;
			}
			if (postSnapPos1 == Vector3.zero)
			{
				lineRenderer.SetPosition(0, joint.attachedRigidbody.transform.TransformPoint(joint.anchor));
			}
			else
			{
				lineRenderer.SetPosition(0, postSnapPos1);
			}
			if ((bool)joint.connectedBody)
			{
				if (!joint.connectedBody.gameObject.activeSelf)
				{
					vel2 = FRILerp.Lerp(vel2, (lineRenderer.GetPosition(0) - lineRenderer.GetPosition(1)) * 15f * Mathf.Clamp(sinceBreak * 2f, 0f, 1f), 10f);
					postSnapPos2 += vel2 * Time.deltaTime;
				}
				else
				{
					vel2 = joint.connectedBody.velocity * 1.5f;
				}
				if (postSnapPos2 == Vector3.zero)
				{
					lineRenderer.SetPosition(1, joint.connectedBody.transform.TransformPoint(joint.connectedAnchor));
				}
				else
				{
					lineRenderer.SetPosition(1, postSnapPos2);
				}
			}
			else if (postSnapPos2 == Vector3.zero)
			{
				lineRenderer.SetPosition(1, joint.connectedAnchor);
			}
			else
			{
				lineRenderer.SetPosition(1, postSnapPos2);
			}
		}
		else
		{
			lineRenderer.SetPosition(0, Vector3.up * 200f);
			lineRenderer.SetPosition(1, Vector3.up * 200f);
		}
		if (!soundRopePlay || lineRenderer.positionCount < 1)
		{
			return;
		}
		if (!soundInitialized)
		{
			soundInitialized = true;
			soundRopeLengthLast = Vector3.Distance(lineRenderer.GetPosition(0), lineRenderer.GetPosition(1));
		}
		soundRopeLengthCurrent = Vector3.Distance(lineRenderer.GetPosition(0), lineRenderer.GetPosition(1));
		soundRopeLengthVelocity = Mathf.Abs(soundRopeLengthLast - soundRopeLengthCurrent);
		soundParameterIntensity.intensity = soundRopeLengthVelocity;
		if (soundRopeLengthVelocity > 0.03f)
		{
			if (!soundIsPlaying)
			{
				soundIsPlaying = true;
				SoundManager.Instance.PlayAtPosition(soundRopeLoop, SoundManager.Instance.transform, base.transform, soundParameterIntensity);
			}
		}
		else if (soundIsPlaying)
		{
			soundIsPlaying = false;
			soundInitialized = false;
			SoundManager.Instance.StopAtPosition(soundRopeLoop, base.transform);
		}
		soundRopeLengthLast = Vector3.Distance(lineRenderer.GetPosition(0), lineRenderer.GetPosition(1));
	}
}
