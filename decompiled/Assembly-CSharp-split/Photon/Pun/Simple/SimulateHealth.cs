using UnityEngine;

namespace Photon.Pun.Simple;

public class SimulateHealth : MonoBehaviour
{
	private Vitals vitals;

	public VitalNameType vitalType = new VitalNameType(VitalType.Health);

	public KeyCode AddHealthKey = KeyCode.Alpha6;

	public KeyCode RemHealthKey = KeyCode.Alpha7;

	public KeyCode DamagehKey = KeyCode.Alpha8;

	public float amount = 20f;

	private void Start()
	{
		IVitalsSystem componentInChildren = GetComponentInChildren<IVitalsSystem>();
		if (componentInChildren == null || !(componentInChildren as SyncObject).PhotonView.IsMine)
		{
			Object.Destroy(this);
		}
		else
		{
			vitals = componentInChildren.Vitals;
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(AddHealthKey))
		{
			vitals.ApplyCharges(vitalType, amount, allowOverload: false, propagate: false);
		}
		if (Input.GetKeyDown(RemHealthKey))
		{
			vitals.ApplyCharges(vitalType, 0f - amount, allowOverload: false, propagate: true);
		}
		if (Input.GetKeyDown(DamagehKey))
		{
			vitals.ApplyCharges(amount, allowOverload: false, propagate: true);
		}
	}
}
