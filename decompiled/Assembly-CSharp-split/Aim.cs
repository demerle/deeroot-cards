using UnityEngine;

public class Aim : MonoBehaviour
{
	private GeneralInput input;

	private HoldingObject holdingObject;

	private CharacterData data;

	private Vector3 aimDirection;

	private void Awake()
	{
		input = GetComponent<GeneralInput>();
		data = GetComponent<CharacterData>();
		holdingObject = GetComponentInChildren<HoldingObject>();
	}

	private void Update()
	{
		if ((double)input.aimDirection.magnitude > 0.2)
		{
			aimDirection = input.aimDirection;
		}
		if (input.direction.magnitude > 0.2f && Optionshandler.leftStickAim && input.aimDirection == Vector3.zero)
		{
			aimDirection = input.direction;
		}
		if ((bool)holdingObject)
		{
			if (aimDirection != Vector3.zero)
			{
				holdingObject.transform.rotation = Quaternion.LookRotation(aimDirection);
			}
			data.aimDirection = aimDirection;
		}
	}
}
