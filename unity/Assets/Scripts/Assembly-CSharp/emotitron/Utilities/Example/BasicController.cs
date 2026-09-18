using Photon.Compression;
using Photon.Pun;
using UnityEngine;
using emotitron.Compression;

namespace emotitron.Utilities.Example
{
	public class BasicController : MonoBehaviour
	{
		public enum Timing
		{
			Auto = 0,
			Fixed = 1,
			Update = 2,
			LateUpdate = 3
		}

		private PhotonView pv;

		private Rigidbody rb;

		private Rigidbody2D rb2D;

		[HideInInspector]
		public TransformCrusher TransformCrusherRef;

		private TransformCrusher tc;

		public Timing timing = Timing.Fixed;

		public bool moveRelative = true;

		[Space]
		public KeyCode moveLeft = KeyCode.A;

		public KeyCode moveRight = KeyCode.D;

		public KeyCode moveFwd = KeyCode.W;

		public KeyCode moveBwd = KeyCode.S;

		public KeyCode moveUp = KeyCode.Space;

		public KeyCode moveDn = KeyCode.Z;

		[Space]
		public KeyCode pitchPos = KeyCode.R;

		public KeyCode pitchNeg = KeyCode.C;

		public KeyCode yawPos = KeyCode.E;

		public KeyCode yawNeg = KeyCode.Q;

		public KeyCode rollPos = KeyCode.Alpha4;

		public KeyCode rollNeg = KeyCode.Alpha4;

		[Space]
		public bool clampToCrusher;

		public float moveSpeed = 5f;

		public float turnSpeed = 60f;

		public float moveForce = 12f;

		public float turnForce = 100f;

		public float scaleSpeed = 1f;

		private bool isMine;

		private bool IsMine
		{
			get
			{
				if (!(pv == null))
				{
					return pv.IsMine;
				}
				return true;
			}
		}

		private void Awake()
		{
			rb = GetComponent<Rigidbody>();
			rb2D = GetComponent<Rigidbody2D>();
			pv = GetComponent<PhotonView>();
		}

		private void Start()
		{
			if (GetComponent<IHasTransformCrusher>() != null)
			{
				tc = GetComponent<IHasTransformCrusher>().TC;
			}
			if (!IsMine)
			{
				if ((bool)rb)
				{
					rb.isKinematic = true;
				}
				if ((bool)rb2D)
				{
					rb2D.isKinematic = true;
				}
			}
		}

		private void FixedUpdate()
		{
			if (timing == Timing.Fixed || (timing == Timing.Auto && (bool)rb))
			{
				Apply();
			}
		}

		private void Update()
		{
			if (timing == Timing.Update || (timing == Timing.Auto && !rb))
			{
				Apply();
			}
		}

		private void LateUpdate()
		{
			if (timing == Timing.LateUpdate)
			{
				Apply();
			}
		}

		private void SumKeys(out Vector3 move, out Vector3 turn)
		{
			move = new Vector3(0f, 0f, 0f);
			if (Input.touchCount > 0)
			{
				Vector2 rawPosition = Input.GetTouch(0).rawPosition;
				if (rawPosition.x < (float)Screen.width * 0.333f)
				{
					move.x -= 1f;
				}
				else if (rawPosition.x > (float)Screen.width * 0.666f)
				{
					move.x += 1f;
				}
				if (rawPosition.y < (float)Screen.height * 0.333f)
				{
					move.z -= 1f;
				}
				else if (rawPosition.y > (float)Screen.height * 0.666f)
				{
					move.z += 1f;
				}
			}
			if (Input.GetKey(moveRight))
			{
				move.x += 1f;
			}
			if (Input.GetKey(moveLeft))
			{
				move.x -= 1f;
			}
			if (Input.GetKey(moveUp))
			{
				move.y += 1f;
			}
			if (Input.GetKey(moveDn))
			{
				move.y -= 1f;
			}
			if (Input.GetKey(moveFwd))
			{
				move.z += 1f;
			}
			if (Input.GetKey(moveBwd))
			{
				move.z -= 1f;
			}
			move = Vector3.ClampMagnitude(move, 1f);
			turn = new Vector3(0f, 0f, 0f);
			if (Input.GetKey(pitchPos))
			{
				turn.x += 1f;
			}
			if (Input.GetKey(pitchNeg))
			{
				turn.x -= 1f;
			}
			if (Input.GetKey(yawPos))
			{
				turn.y += 1f;
			}
			if (Input.GetKey(yawNeg))
			{
				turn.y -= 1f;
			}
			if (Input.GetKey(rollPos))
			{
				turn.z += 1f;
			}
			if (Input.GetKey(rollNeg))
			{
				turn.z -= 1f;
			}
		}

		private void Apply()
		{
			if (!IsMine)
			{
				return;
			}
			Vector3 move;
			Vector3 turn;
			SumKeys(out move, out turn);
			if ((bool)rb && !rb.isKinematic)
			{
				if ((bool)rb && clampToCrusher && tc != null)
				{
					rb.MovePosition(tc.PosCrusher.Clamp(rb.position));
				}
				move *= moveForce * Time.deltaTime;
				if (moveRelative)
				{
					rb.AddRelativeForce(move, ForceMode.VelocityChange);
				}
				else
				{
					rb.AddForce(move, ForceMode.VelocityChange);
				}
			}
			else if ((bool)rb2D && !rb2D.isKinematic)
			{
				if ((bool)rb2D && clampToCrusher && tc != null)
				{
					rb2D.MovePosition(tc.PosCrusher.Clamp(rb2D.position));
				}
				move *= moveForce * Time.deltaTime;
				if (moveRelative)
				{
					rb2D.AddRelativeForce(move, ForceMode2D.Impulse);
				}
				else
				{
					rb2D.AddForce(move, ForceMode2D.Impulse);
				}
			}
			else
			{
				Vector3 vector = (rb ? rb.position : base.transform.position);
				if (moveRelative)
				{
					vector += base.transform.localRotation * move * moveSpeed * Time.deltaTime;
				}
				else
				{
					vector += move * moveSpeed * Time.deltaTime;
				}
				if (clampToCrusher && tc != null && tc.PosCrusher != null)
				{
					vector = tc.PosCrusher.Clamp(vector);
				}
				if ((bool)rb)
				{
					rb.MovePosition(vector);
				}
				else
				{
					base.transform.position = vector;
				}
			}
			if ((bool)rb && !rb.isKinematic)
			{
				turn *= turnForce * Time.deltaTime;
				rb.AddRelativeTorque(turn, ForceMode.VelocityChange);
			}
			else if (clampToCrusher && tc != null && tc.RotCrusher.TRSType != TRSType.Quaternion)
			{
				Vector3 localEulerAngles = tc.RotCrusher.Clamp(base.transform.eulerAngles += turn * turnSpeed * Time.deltaTime);
				base.transform.localEulerAngles = localEulerAngles;
			}
			else
			{
				base.transform.rotation *= Quaternion.Euler(turn * turnSpeed * Time.deltaTime);
			}
		}
	}
}
