using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapManager : MonoBehaviour
{
	[SerializeField]
	public string[] levels;

	public int currentLevelID;

	public static MapManager instance;

	[HideInInspector]
	public bool isTestingMap;

	public MapWrapper currentMap;

	private PhotonView view;

	internal int otherPlayersMostRecentlyLoadedLevel = -1;

	public string forceMap = "";

	private bool callInNextMap;

	private void Awake()
	{
		instance = this;
		view = GetComponent<PhotonView>();
	}

	internal void ReportMapLoaded(int levelID)
	{
		view.RPC("RPCA_ReportMapLoaded", RpcTarget.Others, levelID);
	}

	[PunRPC]
	internal void RPCA_ReportMapLoaded(int levelID)
	{
		otherPlayersMostRecentlyLoadedLevel = levelID;
	}

	private string GetRandomMap()
	{
		if (forceMap != "")
		{
			return forceMap;
		}
		int num = Random.Range(0, levels.Length);
		while (num == currentLevelID && levels.Length > 1)
		{
			num = Random.Range(0, levels.Length);
		}
		return levels[num];
	}

	public void LoadNextLevel(bool callInImidetly = false, bool forceLoad = false)
	{
		if (forceLoad || PhotonNetwork.IsMasterClient || PhotonNetwork.OfflineMode)
		{
			view.RPC("RPCA_SetCallInNextMap", RpcTarget.All, callInImidetly);
			view.RPC("RPCA_LoadLevel", RpcTarget.All, GetRandomMap());
		}
	}

	[PunRPC]
	private void RPCA_SetCallInNextMap(bool toSet)
	{
		callInNextMap = toSet;
	}

	public void LoadLevelFromID(int ID, bool onlyMaster = false, bool callInImidetly = false)
	{
		if (!(!PhotonNetwork.IsMasterClient && onlyMaster))
		{
			callInNextMap = callInImidetly;
			RPCA_LoadLevel(levels[ID]);
		}
	}

	[PunRPC]
	public void RPCA_CallInNewMapAndMovePlayers(int mapID)
	{
		StartCoroutine(WaitForMapToBeLoaded(mapID));
	}

	private IEnumerator WaitForMapToBeLoaded(int mapID)
	{
		while (currentLevelID != mapID)
		{
			yield return null;
		}
		Debug.Log("CALL IN NEW MAP AND MOVE PLAYERS");
		if (currentMap != null)
		{
			MapTransition.instance.Enter(currentMap.Map);
		}
		MapTransition.instance.ClearObjects();
		PlayerManager.instance.RPCA_MovePlayers();
	}

	public void CallInNewMapAndMovePlayers(int mapID)
	{
		if (PhotonNetwork.IsMasterClient || PhotonNetwork.OfflineMode)
		{
			view.RPC("RPCA_CallInNewMapAndMovePlayers", RpcTarget.All, mapID);
		}
	}

	[PunRPC]
	public void RPCA_CallInNewMap()
	{
		if (currentMap != null)
		{
			MapTransition.instance.Enter(currentMap.Map);
		}
		MapTransition.instance.ClearObjects();
	}

	public void CallInNewMap()
	{
		if (PhotonNetwork.IsMasterClient || PhotonNetwork.OfflineMode)
		{
			view.RPC("RPCA_CallInNewMap", RpcTarget.All);
		}
	}

	public SpawnPoint[] GetSpawnPoints()
	{
		return currentMap.Map.GetComponentsInChildren<SpawnPoint>();
	}

	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		Map map = null;
		for (int i = 0; i < scene.GetRootGameObjects().Length; i++)
		{
			map = scene.GetRootGameObjects()[i].GetComponent<Map>();
			if ((bool)map)
			{
				break;
			}
		}
		if (!map)
		{
			Debug.LogError("NO MAP WAS FOUND WHEN LOADING NEW MAP");
		}
		map.wasSpawned = true;
		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
		if (currentMap != null)
		{
			StartCoroutine(UnloadAfterSeconds(currentMap.Scene));
			MapTransition.instance.Exit(currentMap.Map);
		}
		MapTransition.instance.SetStartPos(map);
		map.levelID = currentLevelID;
		currentMap = new MapWrapper(map, scene);
		currentLevelID = GetIDFromScene(scene);
		if (callInNextMap)
		{
			CallInNewMap();
			callInNextMap = false;
		}
		Debug.Log("FINISHED LOADING SCENE");
	}

	private int GetIDFromScene(Scene scene)
	{
		int result = -1;
		for (int i = 0; i < levels.Length; i++)
		{
			if (levels[i] == scene.name)
			{
				result = i;
			}
		}
		return result;
	}

	[PunRPC]
	public void RPCA_LoadLevel(string sceneName)
	{
		Debug.Log("LOADING SCENE");
		SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
		SceneManager.sceneLoaded += OnLevelFinishedLoading;
	}

	private IEnumerator UnloadAfterSeconds(Scene scene)
	{
		yield return new WaitForSecondsRealtime(2f);
		SceneManager.UnloadSceneAsync(scene);
	}

	public void UnloadScene(Scene scene)
	{
		SceneManager.UnloadSceneAsync(scene);
	}
}
