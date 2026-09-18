using System.Collections;
using Photon.Pun;
using Sonigon;
using UnityEngine;

public class CardChoiceVisuals : MonoBehaviour
{
	[Header("Sounds")]
	public SoundEvent soundCardSwitch;

	public static CardChoiceVisuals instance;

	[Header("Settings")]
	public int currentCardSelected;

	public GameObject cardParent;

	public Transform leftHandTarget;

	public Transform rightHandTarget;

	public Transform shieldGem;

	private Vector3 leftHandRestPos;

	private Vector3 rightHandRestPos;

	private Vector3 leftHandVel;

	private Vector3 rightHandVel;

	public float spring = 40f;

	public float drag = 10f;

	public float sway = 1f;

	public float swaySpeed = 1f;

	public int framesToSnap;

	private bool isShowinig;

	private GameObject currentSkin;

	private void Awake()
	{
		instance = this;
		leftHandRestPos = leftHandTarget.position;
		rightHandRestPos = rightHandTarget.position;
	}

	private void Start()
	{
	}

	private void Update()
	{
		if (!isShowinig || Time.unscaledDeltaTime > 0.1f || currentCardSelected >= cardParent.transform.childCount || currentCardSelected < 0)
		{
			return;
		}
		if (rightHandTarget.position.x == float.NaN || rightHandTarget.position.y == float.NaN || rightHandTarget.position.z == float.NaN)
		{
			rightHandTarget.position = Vector3.zero;
			rightHandVel = Vector3.zero;
		}
		if (leftHandTarget.position.x == float.NaN || leftHandTarget.position.y == float.NaN || leftHandTarget.position.z == float.NaN)
		{
			leftHandTarget.position = Vector3.zero;
			leftHandVel = Vector3.zero;
		}
		GameObject obj = cardParent.transform.GetChild(currentCardSelected).gameObject;
		Vector3 zero = Vector3.zero;
		zero = obj.transform.GetChild(0).position;
		if (currentCardSelected < 2)
		{
			leftHandVel += (zero - leftHandTarget.position) * spring * Time.unscaledDeltaTime;
			leftHandVel -= leftHandVel * Time.unscaledDeltaTime * drag;
			rightHandVel += (rightHandRestPos - rightHandTarget.position) * spring * Time.unscaledDeltaTime * 0.5f;
			rightHandVel -= rightHandVel * Time.unscaledDeltaTime * drag * 0.5f;
			rightHandVel += sway * new Vector3(-0.5f + Mathf.PerlinNoise(Time.unscaledTime * swaySpeed, 0f), -0.5f + Mathf.PerlinNoise(Time.unscaledTime * swaySpeed + 100f, 0f), 0f) * Time.unscaledDeltaTime;
			shieldGem.transform.position = rightHandTarget.position;
			if (framesToSnap > 0)
			{
				leftHandTarget.position = zero;
			}
		}
		else
		{
			rightHandVel += (zero - rightHandTarget.position) * spring * Time.unscaledDeltaTime;
			rightHandVel -= rightHandVel * Time.unscaledDeltaTime * drag;
			leftHandVel += (leftHandRestPos - leftHandTarget.position) * spring * Time.unscaledDeltaTime * 0.5f;
			leftHandVel -= leftHandVel * Time.unscaledDeltaTime * drag * 0.5f;
			leftHandVel += sway * new Vector3(-0.5f + Mathf.PerlinNoise(Time.unscaledTime * swaySpeed, Time.unscaledTime * swaySpeed), -0.5f + Mathf.PerlinNoise(Time.unscaledTime * swaySpeed + 100f, Time.unscaledTime * swaySpeed + 100f), 0f) * Time.unscaledDeltaTime;
			shieldGem.transform.position = leftHandTarget.position;
			if (framesToSnap > 0)
			{
				rightHandTarget.position = zero;
			}
		}
		framesToSnap--;
		leftHandTarget.position += leftHandVel * Time.unscaledDeltaTime;
		rightHandTarget.position += rightHandVel * Time.unscaledDeltaTime;
	}

	public void Show(int pickerID = 0, bool animateIn = false)
	{
		isShowinig = true;
		base.transform.GetChild(0).gameObject.SetActive(true);
		if (animateIn)
		{
			GetComponent<CurveAnimation>().PlayIn();
		}
		else
		{
			base.transform.localScale = Vector3.one * 33f;
		}
		if ((bool)currentSkin)
		{
			Object.Destroy(currentSkin);
		}
		if (PlayerManager.instance.players[pickerID].data.view.IsMine)
		{
			PlayerFace playerFace = null;
			playerFace = ((!PhotonNetwork.OfflineMode) ? CharacterCreatorHandler.instance.selectedPlayerFaces[0] : CharacterCreatorHandler.instance.selectedPlayerFaces[pickerID]);
			GetComponent<PhotonView>().RPC("RPCA_SetFace", RpcTarget.All, playerFace.eyeID, playerFace.eyeOffset, playerFace.mouthID, playerFace.mouthOffset, playerFace.detailID, playerFace.detailOffset, playerFace.detail2ID, playerFace.detail2Offset);
		}
		currentSkin = Object.Instantiate(PlayerSkinBank.GetPlayerSkinColors(pickerID).gameObject, base.transform.GetChild(0).transform.position, Quaternion.identity, base.transform.GetChild(0).transform);
		currentSkin.GetComponentInChildren<ParticleSystem>().Play();
		leftHandTarget.position = base.transform.GetChild(0).position;
		rightHandTarget.position = base.transform.GetChild(0).position;
		leftHandVel *= 0f;
		rightHandVel *= 0f;
		StopAllCoroutines();
	}

	public void Hide()
	{
		GetComponent<CurveAnimation>().PlayOut();
		StartCoroutine(DelayHide());
	}

	private IEnumerator DelayHide()
	{
		yield return new WaitForSecondsRealtime(0.3f);
		base.transform.GetChild(0).gameObject.SetActive(false);
		isShowinig = false;
	}

	internal void SetCurrentSelected(int toSet)
	{
		GetComponent<PhotonView>().RPC("RPCA_SetCurrentSelected", RpcTarget.All, toSet);
	}

	[PunRPC]
	internal void RPCA_SetCurrentSelected(int toSet)
	{
		SoundManager.Instance.Play(soundCardSwitch, base.transform);
		currentCardSelected = toSet;
	}
}
