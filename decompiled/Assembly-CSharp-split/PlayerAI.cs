using UnityEngine;

public class PlayerAI : MonoBehaviour
{
	private float range = 6f;

	private CharacterData data;

	private GeneralInput input;

	private Vector3 moveDir = Vector3.zero;

	private Vector3 aimDir = Vector3.zero;

	private Vector3 targetPos;

	private Player target;

	private bool canSeeTarget;

	private float untilNextDataUpdate;

	private float getRandomTargetPosCounter;

	private bool isShooting;

	private float distanceToTarget = 5f;

	private void Start()
	{
		data = GetComponentInParent<CharacterData>();
		input = data.input;
	}

	private void Update()
	{
		target = PlayerManager.instance.GetOtherPlayer(data.player);
		untilNextDataUpdate -= TimeHandler.deltaTime;
		if (!target)
		{
			return;
		}
		canSeeTarget = PlayerManager.instance.CanSeePlayer(base.transform.position, target).canSee;
		input.ResetInput();
		if (!canSeeTarget)
		{
			getRandomTargetPosCounter -= TimeHandler.deltaTime;
			if (getRandomTargetPosCounter < 0f)
			{
				getRandomTargetPosCounter = Random.Range(0.5f, 2f);
				GetRandomPos();
			}
		}
		if (untilNextDataUpdate <= 0f)
		{
			if (Random.value < 0.25f / Mathf.Clamp(distanceToTarget * 0.1f, 0.1f, 10f) && canSeeTarget)
			{
				input.shieldWasPressed = true;
			}
			if (Random.value < 0.4f && canSeeTarget)
			{
				isShooting = true;
			}
			else
			{
				isShooting = false;
			}
			if (Random.value < 0.2f || data.isWallGrab)
			{
				input.jumpWasPressed = true;
			}
			untilNextDataUpdate = Random.Range(0f, 0.25f);
			UpdateData();
		}
		input.shootIsPressed = isShooting;
		input.shootWasPressed = isShooting;
		input.aimDirection = aimDir;
		input.direction = moveDir;
	}

	private void GetRandomPos()
	{
		Vector3 vector = Vector3.zero;
		int num = 200;
		while (vector == Vector3.zero && num > 0)
		{
			num--;
			Vector3 vector2 = base.transform.position + Vector3.up * 5f + (Vector3)Random.insideUnitCircle * 15f;
			if (data.ThereIsGroundBelow(vector2, 8f))
			{
				vector = vector2;
			}
		}
		targetPos = vector;
	}

	private void UpdateData()
	{
		if (canSeeTarget)
		{
			targetPos = target.transform.position;
		}
		distanceToTarget = Vector3.Distance(base.transform.position, target.transform.position);
		aimDir = (targetPos - base.transform.position).normalized;
		moveDir = aimDir;
		if (moveDir.x > 0f)
		{
			moveDir.x = 1f;
		}
		if (moveDir.x < 0f)
		{
			moveDir.x = -1f;
		}
		if (canSeeTarget && distanceToTarget < range && data.ThereIsGroundBelow(base.transform.position, 10f))
		{
			moveDir = Vector3.zero;
		}
	}
}
