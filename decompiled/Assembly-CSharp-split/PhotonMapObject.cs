using System;
using System.Collections;
using Photon.Pun;
using UnityEngine;

public class PhotonMapObject : MonoBehaviour
{
	private Map map;

	private bool photonSpawned;

	private float counter;

	private bool waitingToBeRemoved;

	private void Awake()
	{
		if (base.transform.parent != null)
		{
			UnityEngine.Object.DestroyImmediate(GetComponent<PhotonView>());
		}
	}

	private void Start()
	{
		Rigidbody2D component = GetComponent<Rigidbody2D>();
		component.isKinematic = true;
		component.simulated = false;
		if (base.transform.parent == null)
		{
			photonSpawned = true;
			base.transform.SetParent(MapManager.instance.currentMap.Map.transform, worldPositionStays: true);
			map = GetComponentInParent<Map>();
			map.missingObjects--;
			Map obj = map;
			obj.mapIsReadyAction = (Action)Delegate.Combine(obj.mapIsReadyAction, new Action(Go));
			if (map.hasRope && !GetComponent<PhotonView>().IsMine)
			{
				component.gravityScale = 0f;
			}
		}
		else
		{
			map = GetComponentInParent<Map>();
			Map obj2 = map;
			obj2.mapIsReadyEarlyAction = (Action)Delegate.Combine(obj2.mapIsReadyEarlyAction, new Action(GoEarly));
		}
	}

	private void GoEarly()
	{
		if (waitingToBeRemoved)
		{
			UnityEngine.Object.DestroyImmediate(base.gameObject);
		}
	}

	private void Go()
	{
		StartCoroutine(IGo());
	}

	private IEnumerator IGo()
	{
		Rigidbody2D rig = GetComponent<Rigidbody2D>();
		yield return new WaitForSeconds(0f);
		yield return new WaitForSeconds(0f);
		yield return new WaitForSeconds(0f);
		rig.isKinematic = false;
		rig.simulated = true;
		if ((bool)rig)
		{
			for (float i = 0f; i < 1f; i += Time.deltaTime * 1f)
			{
				rig.velocity -= rig.velocity * i * 0.05f;
				yield return null;
			}
		}
	}

	private void Update()
	{
		if (waitingToBeRemoved || photonSpawned)
		{
			return;
		}
		counter += Mathf.Clamp(Time.deltaTime, 0f, 0.1f);
		if ((PhotonNetwork.OfflineMode && counter > 1f && map.hasEntered) || ((bool)map && map.hasEntered && map.LoadedForAll()))
		{
			if (PhotonNetwork.IsMasterClient)
			{
				PhotonNetwork.Instantiate("4 Map Objects/" + base.gameObject.name.Split(char.Parse(" "))[0], base.transform.position, base.transform.rotation, 0);
			}
			map.missingObjects++;
			waitingToBeRemoved = true;
		}
	}
}
