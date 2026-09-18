using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using SoundImplementation;
using UnityEngine;

public class CardChoice : MonoBehaviour
{
	private enum StickDirection
	{
		Left,
		Right,
		None
	}

	public int pickrID = -1;

	public ArtInstance cardPickArt;

	private Transform[] children;

	public CardInfo[] cards;

	public int picks = 6;

	public static CardChoice instance;

	public bool IsPicking;

	private List<GameObject> spawnedCards = new List<GameObject>();

	public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	private float speed = 4f;

	private bool isPlaying;

	private StickDirection lastStickDirection;

	private int currentlySelectedCard;

	private float counter = 1f;

	private PickerType pickerType;

	public CardThemeColor[] cardThemes;

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		for (int i = 0; i < cards.Length; i++)
		{
			PhotonNetwork.PrefabPool.RegisterPrefab(cards[i].gameObject.name, cards[i].gameObject);
		}
		children = new Transform[base.transform.childCount];
		for (int j = 0; j < children.Length; j++)
		{
			children[j] = base.transform.GetChild(j);
		}
	}

	public CardInfo GetSourceCard(CardInfo info)
	{
		for (int i = 0; i < cards.Length; i++)
		{
			if (cards[i].cardName == info.cardName)
			{
				return cards[i];
			}
		}
		return null;
	}

	public void Pick(GameObject pickedCard = null, bool clear = false)
	{
		if ((bool)pickedCard)
		{
			pickedCard.GetComponentInChildren<ApplyCardStats>().Pick(pickrID, forcePick: false, pickerType);
			GetComponent<PhotonView>().RPC("RPCA_DoEndPick", RpcTarget.All, CardIDs(), pickedCard.GetComponent<PhotonView>().ViewID, pickedCard.GetComponent<PublicInt>().theInt, pickrID);
		}
		else if (PlayerManager.instance.GetPlayerWithID(pickrID).data.view.IsMine)
		{
			StartCoroutine(ReplaceCards(pickedCard, clear));
		}
	}

	private int[] CardIDs()
	{
		int[] array = new int[spawnedCards.Count];
		for (int i = 0; i < spawnedCards.Count; i++)
		{
			array[i] = spawnedCards[i].GetComponent<PhotonView>().ViewID;
		}
		return array;
	}

	private List<GameObject> CardFromIDs(int[] cardIDs)
	{
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < cardIDs.Length; i++)
		{
			list.Add(PhotonNetwork.GetPhotonView(cardIDs[i]).gameObject);
		}
		return list;
	}

	[PunRPC]
	private void RPCA_DoEndPick(int[] cardIDs, int targetCardID, int theInt = 0, int pickId = -1)
	{
		GameObject pickedCard = PhotonNetwork.GetPhotonView(targetCardID).gameObject;
		spawnedCards = CardFromIDs(cardIDs);
		StartCoroutine(IDoEndPick(pickedCard, theInt, pickId));
	}

	public IEnumerator IDoEndPick(GameObject pickedCard = null, int theInt = 0, int pickId = -1)
	{
		Vector3 startPos = pickedCard.transform.position;
		Vector3 endPos = CardChoiceVisuals.instance.transform.position;
		float c = 0f;
		while (c < 1f)
		{
			CardChoiceVisuals.instance.framesToSnap = 1;
			Vector3 position = Vector3.LerpUnclamped(startPos, endPos, curve.Evaluate(c));
			pickedCard.transform.position = position;
			base.transform.GetChild(theInt).position = position;
			c += Time.deltaTime * speed;
			yield return null;
		}
		GamefeelManager.GameFeel((startPos - endPos).normalized * 2f);
		for (int i = 0; i < spawnedCards.Count; i++)
		{
			if ((bool)spawnedCards[i])
			{
				if (spawnedCards[i].gameObject != pickedCard)
				{
					spawnedCards[i].AddComponent<Rigidbody>().AddForce((spawnedCards[i].transform.position - endPos) * Random.Range(0f, 50f));
					spawnedCards[i].GetComponent<Rigidbody>().AddTorque(Random.onUnitSphere * Random.Range(0f, 200f));
					spawnedCards[i].AddComponent<RemoveAfterSeconds>().seconds = Random.Range(0.5f, 1f);
					spawnedCards[i].GetComponent<RemoveAfterSeconds>().shrink = true;
				}
				else
				{
					spawnedCards[i].GetComponentInChildren<CardVisuals>().Leave();
				}
			}
		}
		yield return new WaitForSeconds(0.25f);
		AnimationCurve softCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		Vector3 startPos2 = base.transform.GetChild(theInt).transform.position;
		Vector3 endPos2 = startPos;
		c = 0f;
		while (c < 1f)
		{
			Vector3 position2 = Vector3.LerpUnclamped(startPos2, endPos2, softCurve.Evaluate(c));
			base.transform.GetChild(theInt).position = position2;
			c += Time.deltaTime * speed * 1.5f;
			yield return null;
		}
		SoundPlayerStatic.Instance.PlayPlayerBallDisappear();
		base.transform.GetChild(theInt).position = startPos;
		spawnedCards.Clear();
		if (PlayerManager.instance.GetPlayerWithID(pickId).data.view.IsMine)
		{
			StartCoroutine(ReplaceCards(pickedCard));
		}
	}

	private GameObject Spawn(GameObject objToSpawn, Vector3 pos, Quaternion rot)
	{
		return PhotonNetwork.Instantiate(GetCardPath(objToSpawn), pos, rot, 0);
	}

	private string GetCardPath(GameObject targetObj)
	{
		return targetObj.name;
	}

	public GameObject AddCard(CardInfo cardToSpawn)
	{
		GameObject gameObject = Spawn(cardToSpawn.gameObject, new Vector3(30f, -10f, 0f), Quaternion.identity);
		spawnedCards.Add(gameObject);
		return gameObject;
	}

	public GameObject AddCardVisual(CardInfo cardToSpawn, Vector3 pos)
	{
		GameObject obj = Object.Instantiate(cardToSpawn.gameObject, pos, Quaternion.identity);
		obj.GetComponentInChildren<CardVisuals>().firstValueToSet = true;
		return obj;
	}

	private IEnumerator ReplaceCards(GameObject pickedCard = null, bool clear = false)
	{
		if (picks > 0)
		{
			SoundPlayerStatic.Instance.PlayPlayerBallAppear();
		}
		isPlaying = true;
		if (clear && spawnedCards != null)
		{
			for (int i = 0; i < spawnedCards.Count; i++)
			{
				if (pickedCard != spawnedCards[i])
				{
					spawnedCards[i].GetComponentInChildren<CardVisuals>().Leave();
					yield return new WaitForSecondsRealtime(0.1f);
				}
			}
			yield return new WaitForSecondsRealtime(0.2f);
			if ((bool)pickedCard)
			{
				pickedCard.GetComponentInChildren<CardVisuals>().Pick();
			}
			spawnedCards.Clear();
		}
		yield return new WaitForSecondsRealtime(0.2f);
		if (picks > 0)
		{
			for (int i = 0; i < children.Length; i++)
			{
				spawnedCards.Add(SpawnUniqueCard(children[i].transform.position, children[i].transform.rotation));
				spawnedCards[i].AddComponent<PublicInt>().theInt = i;
				yield return new WaitForSecondsRealtime(0.1f);
			}
		}
		else
		{
			GetComponent<PhotonView>().RPC("RPCA_DonePicking", RpcTarget.All);
		}
		picks--;
		isPlaying = false;
	}

	[PunRPC]
	private void RPCA_DonePicking()
	{
		IsPicking = false;
	}

	private GameObject GetRanomCard()
	{
		GameObject result = null;
		float num = 0f;
		for (int i = 0; i < cards.Length; i++)
		{
			if (cards[i].rarity == CardInfo.Rarity.Common)
			{
				num += 10f;
			}
			if (cards[i].rarity == CardInfo.Rarity.Uncommon)
			{
				num += 4f;
			}
			if (cards[i].rarity == CardInfo.Rarity.Rare)
			{
				num += 1f;
			}
		}
		float num2 = Random.Range(0f, num);
		for (int j = 0; j < cards.Length; j++)
		{
			if (cards[j].rarity == CardInfo.Rarity.Common)
			{
				num2 -= 10f;
			}
			if (cards[j].rarity == CardInfo.Rarity.Uncommon)
			{
				num2 -= 4f;
			}
			if (cards[j].rarity == CardInfo.Rarity.Rare)
			{
				num2 -= 1f;
			}
			if (num2 <= 0f)
			{
				result = cards[j].gameObject;
				break;
			}
		}
		return result;
	}

	private GameObject SpawnUniqueCard(Vector3 pos, Quaternion rot)
	{
		GameObject ranomCard = GetRanomCard();
		CardInfo component = ranomCard.GetComponent<CardInfo>();
		Player player = null;
		player = ((pickerType != PickerType.Team) ? PlayerManager.instance.players[pickrID] : PlayerManager.instance.GetPlayersInTeam(pickrID)[0]);
		for (int i = 0; i < spawnedCards.Count; i++)
		{
			bool flag = spawnedCards[i].GetComponent<CardInfo>().cardName == ranomCard.GetComponent<CardInfo>().cardName;
			if (pickrID != -1)
			{
				Holdable holdable = player.data.GetComponent<Holding>().holdable;
				if ((bool)holdable)
				{
					Gun component2 = holdable.GetComponent<Gun>();
					Gun component3 = ranomCard.GetComponent<Gun>();
					if ((bool)component3 && (bool)component2 && component3.lockGunToDefault && component2.lockGunToDefault)
					{
						flag = true;
					}
				}
				for (int j = 0; j < player.data.currentCards.Count; j++)
				{
					CardInfo component4 = player.data.currentCards[j].GetComponent<CardInfo>();
					for (int k = 0; k < component4.blacklistedCategories.Length; k++)
					{
						for (int l = 0; l < component.categories.Length; l++)
						{
							if (component.categories[l] == component4.blacklistedCategories[k])
							{
								flag = true;
							}
						}
					}
					if (!component4.allowMultiple && component.cardName == component4.cardName)
					{
						flag = true;
					}
				}
			}
			if (flag)
			{
				return SpawnUniqueCard(pos, rot);
			}
		}
		GameObject obj = Spawn(ranomCard.gameObject, pos, rot);
		obj.GetComponent<CardInfo>().sourceCard = ranomCard.GetComponent<CardInfo>();
		obj.GetComponentInChildren<DamagableEvent>().GetComponent<Collider2D>().enabled = false;
		return obj;
	}

	private void Update()
	{
		counter += Time.deltaTime;
		_ = isPlaying;
		if (pickrID != -1 && IsPicking)
		{
			DoPlayerSelect();
		}
		if (Application.isEditor && !DevConsole.isTyping && Input.GetKeyDown(KeyCode.N))
		{
			picks++;
			instance.Pick(null, clear: true);
		}
	}

	private void DoPlayerSelect()
	{
		SoundMusicManager.Instance.PlayIngame(isCard: true);
		if (spawnedCards.Count == 0 || pickrID == -1)
		{
			return;
		}
		PlayerActions[] array = null;
		array = ((pickerType != PickerType.Team) ? PlayerManager.instance.GetActionsFromPlayer(pickrID) : PlayerManager.instance.GetActionsFromTeam(pickrID));
		if (array == null)
		{
			Pick(spawnedCards[0]);
			return;
		}
		StickDirection stickDirection = StickDirection.None;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == null)
			{
				continue;
			}
			if (array[i].Right.Value > 0.7f)
			{
				stickDirection = StickDirection.Right;
			}
			if (array[i].Left.Value > 0.7f)
			{
				stickDirection = StickDirection.Left;
			}
			currentlySelectedCard = Mathf.Clamp(currentlySelectedCard, 0, spawnedCards.Count - 1);
			for (int j = 0; j < spawnedCards.Count; j++)
			{
				if ((bool)spawnedCards[j] && (spawnedCards[j].GetComponentInChildren<CardVisuals>().isSelected != (currentlySelectedCard == j) || counter > 0.2f))
				{
					counter = 0f;
					spawnedCards[j].GetComponent<PhotonView>().RPC("RPCA_ChangeSelected", RpcTarget.All, currentlySelectedCard == j);
				}
			}
			if (array[i].Jump.WasPressed && !isPlaying && spawnedCards[currentlySelectedCard] != null)
			{
				Pick(spawnedCards[currentlySelectedCard]);
				pickrID = -1;
				break;
			}
		}
		if (stickDirection != lastStickDirection)
		{
			if (stickDirection == StickDirection.Left)
			{
				currentlySelectedCard--;
			}
			if (stickDirection == StickDirection.Right)
			{
				currentlySelectedCard++;
			}
			lastStickDirection = stickDirection;
		}
		if (CardChoiceVisuals.instance.currentCardSelected != currentlySelectedCard)
		{
			CardChoiceVisuals.instance.SetCurrentSelected(currentlySelectedCard);
		}
	}

	public IEnumerator DoPick(int picksToSet, int picketIDToSet, PickerType pType = PickerType.Team)
	{
		pickerType = pType;
		StartPick(picksToSet, picketIDToSet);
		while (IsPicking)
		{
			yield return null;
		}
		UIHandler.instance.StopShowPicker();
		CardChoiceVisuals.instance.Hide();
	}

	public void StartPick(int picksToSet, int pickerIDToSet)
	{
		pickrID = pickerIDToSet;
		IsPicking = true;
		picks = picksToSet;
		ArtHandler.instance.SetSpecificArt(cardPickArt);
		Pick();
	}

	public Color GetCardColor(CardThemeColor.CardThemeColorType colorType)
	{
		for (int i = 0; i < cardThemes.Length; i++)
		{
			if (cardThemes[i].themeType == colorType)
			{
				return cardThemes[i].targetColor;
			}
		}
		return Color.black;
	}

	public Color GetCardColor2(CardThemeColor.CardThemeColorType colorType)
	{
		for (int i = 0; i < cardThemes.Length; i++)
		{
			if (cardThemes[i].themeType == colorType)
			{
				return cardThemes[i].bgColor;
			}
		}
		return Color.black;
	}
}
