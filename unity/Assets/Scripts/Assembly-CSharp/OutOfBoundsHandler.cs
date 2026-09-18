using Sonigon;
using UnityEngine;

public class OutOfBoundsHandler : MonoBehaviour
{
	private bool outOfBounds;

	private bool almostOutOfBounds;

	private Camera mainCam;

	private CharacterData data;

	public ParticleSystem wall;

	public ParticleSystem burst;

	public ParticleSystem burstBig;

	public ParticleSystem warning;

	public ParticleSystem shieldWall;

	public ParticleSystem shieldBurst;

	public ParticleSystem shieldBurstBig;

	private ChildRPC rpc;

	private float counter;

	private float warningPercentage = 0.1f;

	private void Start()
	{
		base.transform.position = Vector3.up * 200f;
		data = base.transform.root.GetComponent<CharacterData>();
		rpc = data.GetComponent<ChildRPC>();
		rpc.childRPCs.Add("OutOfBounds", RPCA_DisplayOutOfBounds);
		rpc.childRPCs.Add("ShieldOutOfBounds", RPCA_DisplayOutOfBoundsShield);
		mainCam = MainCam.instance.transform.GetComponent<Camera>();
		base.transform.SetParent(null);
	}

	private void LateUpdate()
	{
		if (!data)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			if (!data.playerVel.simulated || !data.isPlaying)
			{
				return;
			}
			float x = Mathf.InverseLerp(-35.56f, 35.56f, data.transform.position.x);
			float y = Mathf.InverseLerp(-20f, 20f, data.transform.position.y);
			Vector3 vector = new Vector3(x, y, 0f);
			vector = new Vector3(Mathf.Clamp(vector.x, 0f, 1f), Mathf.Clamp(vector.y, 0f, 1f), vector.z);
			almostOutOfBounds = false;
			outOfBounds = false;
			if (vector.x <= 0f || vector.x >= 1f || vector.y >= 1f || vector.y <= 0f)
			{
				outOfBounds = true;
			}
			else if (vector.x < warningPercentage || vector.x > 1f - warningPercentage || vector.y > 1f - warningPercentage || vector.y < warningPercentage)
			{
				almostOutOfBounds = true;
				if (vector.x < warningPercentage)
				{
					vector.x = 0f;
				}
				if (vector.x > 1f - warningPercentage)
				{
					vector.x = 1f;
				}
				if (vector.y < warningPercentage)
				{
					vector.y = 0f;
				}
				if (vector.y > 1f - warningPercentage)
				{
					vector.y = 1f;
				}
			}
			counter += TimeHandler.deltaTime;
			if (almostOutOfBounds && !data.dead)
			{
				base.transform.position = GetPoint(vector);
				base.transform.rotation = Quaternion.LookRotation(Vector3.forward, -(data.transform.position - base.transform.position));
				if (counter > 0.1f)
				{
					counter = 0f;
					warning.Play();
				}
			}
			if (!outOfBounds || data.dead)
			{
				return;
			}
			data.sinceGrounded = 0f;
			base.transform.position = GetPoint(vector);
			base.transform.rotation = Quaternion.LookRotation(Vector3.forward, -(data.transform.position - base.transform.position));
			if (counter > 0.1f && data.view.IsMine)
			{
				counter = 0f;
				if (data.block.IsBlocking())
				{
					rpc.CallFunction("ShieldOutOfBounds");
					data.playerVel.velocity *= 0f;
					data.healthHandler.CallTakeForce(base.transform.up * 400f * data.playerVel.mass, ForceMode2D.Impulse, false, true);
					data.transform.position = base.transform.position;
				}
				else
				{
					rpc.CallFunction("OutOfBounds");
					data.healthHandler.CallTakeForce(base.transform.up * 200f * data.playerVel.mass, ForceMode2D.Impulse, false, true);
					data.healthHandler.CallTakeDamage(51f * base.transform.up, data.transform.position);
				}
			}
		}
	}

	private Vector3 GetPoint(Vector3 p)
	{
		float x = Mathf.Lerp(-35.56f, 35.56f, p.x);
		float y = Mathf.Lerp(-20f, 20f, p.y);
		return new Vector3(x, y, 0f);
	}

	private void RPCA_DisplayOutOfBounds()
	{
		SoundManager.Instance.Play(data.playerSounds.soundCharacterDamageScreenEdge, data.transform);
		burst.Play();
		wall.Play();
		burstBig.Play();
		data.sinceGrounded = 0f;
	}

	private void RPCA_DisplayOutOfBoundsShield()
	{
		SoundManager.Instance.Play(data.playerSounds.soundCharacterDamageScreenEdge, data.transform);
		shieldBurst.Play();
		shieldWall.Play();
		shieldBurstBig.Play();
		data.sinceGrounded = 0f;
	}
}
