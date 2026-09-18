using System;
using System.Collections;
using Photon.Pun;
using UnityEngine;

public class Map : MonoBehaviour
{
	public bool wasSpawned;

	public float size = 15f;

	public bool hasEntered;

	internal int levelID;

	internal int missingObjects;

	private float counter;

	private int readyForFrames;

	private bool hasCalledReady;

	public Rigidbody2D[] allRigs;

	private SpawnPoint[] spawnPoints;

	public Action mapIsReadyAction;

	public Action mapIsReadyEarlyAction;

	public Action mapMovingOutAction;

	internal bool hasRope;

	internal bool LoadedForAll()
	{
		return MapManager.instance.otherPlayersMostRecentlyLoadedLevel == levelID;
	}

	private void Awake()
	{
		if (!GameManager.instance || !GameManager.instance.isPlaying)
		{
			GM_Test componentInChildren = GameManager.instance.transform.root.GetComponentInChildren<GM_Test>(true);
			componentInChildren.gameObject.SetActive(true);
			componentInChildren.testMap = true;
			MapManager.instance.isTestingMap = true;
			componentInChildren.transform.root.Find("UI/UI_MainMenu").gameObject.SetActive(false);
			hasEntered = true;
		}
	}

	private void Update()
	{
		counter += Time.deltaTime;
		if (!hasCalledReady && ((PhotonNetwork.OfflineMode && counter > 1f && hasEntered) || (hasEntered && LoadedForAll())))
		{
			if (missingObjects <= 0)
			{
				readyForFrames++;
			}
			if (readyForFrames > 2)
			{
				StartCoroutine(StartMatch());
			}
		}
	}

	public void MapMoveOut()
	{
		Action action = mapMovingOutAction;
		if (action != null)
		{
			action();
		}
	}

	internal Vector3 GetRandomSpawnPos()
	{
		if (spawnPoints == null)
		{
			spawnPoints = GetComponentsInChildren<SpawnPoint>();
		}
		return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform.position;
	}

	private IEnumerator StartMatch()
	{
		hasCalledReady = true;
		Action action = mapIsReadyEarlyAction;
		if (action != null)
		{
			action();
		}
		yield return new WaitForSecondsRealtime(0f);
		allRigs = GetComponentsInChildren<Rigidbody2D>();
		Action action2 = mapIsReadyAction;
		if (action2 != null)
		{
			action2();
		}
	}

	private void Start()
	{
		if (!PhotonNetwork.OfflineMode)
		{
			MapManager.instance.ReportMapLoaded(levelID);
		}
		if (MapManager.instance.isTestingMap)
		{
			wasSpawned = true;
		}
		if (!wasSpawned)
		{
			MapManager.instance.UnloadScene(base.gameObject.scene);
		}
		SpriteRenderer[] componentsInChildren = GetComponentsInChildren<SpriteRenderer>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if ((double)componentsInChildren[i].color.a < 0.5)
			{
				continue;
			}
			componentsInChildren[i].transform.position = new Vector3(componentsInChildren[i].transform.position.x, componentsInChildren[i].transform.position.y, -3f);
			if (!(componentsInChildren[i].gameObject.tag == "NoMask"))
			{
				componentsInChildren[i].color = new Color(11f / 51f, 11f / 51f, 11f / 51f);
				if (!componentsInChildren[i].GetComponent<SpriteMask>())
				{
					componentsInChildren[i].gameObject.AddComponent<SpriteMask>().sprite = componentsInChildren[i].sprite;
				}
			}
		}
		SpriteMask[] componentsInChildren2 = GetComponentsInChildren<SpriteMask>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			if (!(componentsInChildren2[j].gameObject.tag == "NoMask"))
			{
				componentsInChildren2[j].isCustomRangeActive = true;
				componentsInChildren2[j].frontSortingLayerID = SortingLayer.NameToID("MapParticle");
				componentsInChildren2[j].frontSortingOrder = 1;
				componentsInChildren2[j].backSortingLayerID = SortingLayer.NameToID("MapParticle");
				componentsInChildren2[j].backSortingOrder = 0;
			}
		}
	}
}
