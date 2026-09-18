using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionInstance : MonoBehaviour
{
	public int currentlySelectedFace;

	public Player currentPlayer;

	public GameObject getReadyObj;

	private HoverEvent currentButton;

	private CharacterSelectionInstance[] selectors;

	private HoverEvent[] buttons;

	public bool isReady;

	private float counter;

	private void Start()
	{
		selectors = base.transform.parent.GetComponentsInChildren<CharacterSelectionInstance>();
	}

	public void ResetMenu()
	{
		base.transform.GetChild(0).gameObject.SetActive(false);
		currentPlayer = null;
		getReadyObj.gameObject.SetActive(false);
		PlayerManager.instance.RemovePlayers();
	}

	private void OnEnable()
	{
		if (!base.transform.GetChild(0).gameObject.activeSelf)
		{
			GetComponentInChildren<GeneralParticleSystem>(true).gameObject.SetActive(true);
			GetComponentInChildren<GeneralParticleSystem>(true).Play();
		}
	}

	public void StartPicking(Player pickingPlayer)
	{
		currentPlayer = pickingPlayer;
		currentlySelectedFace = 0;
		GetComponentInChildren<GeneralParticleSystem>(true).gameObject.SetActive(false);
		GetComponentInChildren<GeneralParticleSystem>(true).Stop();
		base.transform.GetChild(0).gameObject.SetActive(true);
		getReadyObj.gameObject.SetActive(true);
		if (currentPlayer.data.input.inputType == GeneralInput.InputType.Keyboard)
		{
			getReadyObj.GetComponent<TextMeshProUGUI>().text = "PRESS [SPACE] WHEN READY";
		}
		else
		{
			getReadyObj.GetComponent<TextMeshProUGUI>().text = "PRESS [START] WHEN READY";
		}
		buttons = base.transform.GetComponentsInChildren<HoverEvent>();
		for (int i = 0; i < buttons.Length; i++)
		{
			if (pickingPlayer.data.input.inputType == GeneralInput.InputType.Controller)
			{
				buttons[i].enabled = false;
				buttons[i].GetComponent<Button>().interactable = false;
				buttons[i].GetComponent<CharacterCreatorPortrait>().controlType = MenuControllerHandler.MenuControl.Controller;
				continue;
			}
			buttons[i].enabled = true;
			buttons[i].GetComponent<Button>().interactable = true;
			buttons[i].GetComponent<CharacterCreatorPortrait>().controlType = MenuControllerHandler.MenuControl.Mouse;
			Navigation navigation = buttons[i].GetComponent<Button>().navigation;
			navigation.mode = Navigation.Mode.None;
			buttons[i].GetComponent<Button>().navigation = navigation;
		}
	}

	public void ReadyUp()
	{
		isReady = !isReady;
		bool flag = true;
		for (int i = 0; i < selectors.Length; i++)
		{
			if (!selectors[i].isReady)
			{
				flag = false;
			}
		}
		if (flag)
		{
			MainMenuHandler.instance.Close();
			GM_ArmsRace.instance.StartGame();
		}
		if (currentPlayer.data.input.inputType == GeneralInput.InputType.Keyboard)
		{
			getReadyObj.GetComponent<TextMeshProUGUI>().text = (isReady ? "READY" : "PRESS [SPACE] WHEN READY");
		}
		else
		{
			getReadyObj.GetComponent<TextMeshProUGUI>().text = (isReady ? "READY" : "PRESS [START] WHEN READY");
		}
	}

	private void Update()
	{
		if (!currentPlayer)
		{
			return;
		}
		if (currentPlayer.data.input.inputType != 0)
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				ReadyUp();
			}
			return;
		}
		if (currentPlayer.data.playerActions.Device.CommandWasPressed)
		{
			ReadyUp();
		}
		HoverEvent component = buttons[currentlySelectedFace].GetComponent<HoverEvent>();
		if (currentButton != component)
		{
			if ((bool)currentButton)
			{
				currentButton.GetComponent<SimulatedSelection>().Deselect();
			}
			currentButton = component;
			currentButton.GetComponent<SimulatedSelection>().Select();
		}
		counter += Time.deltaTime;
		if (Mathf.Abs(currentPlayer.data.playerActions.Move.X) > 0.5f && counter > 0.2f)
		{
			if (currentPlayer.data.playerActions.Move.X > 0.5f)
			{
				currentlySelectedFace++;
			}
			else
			{
				currentlySelectedFace--;
			}
			counter = 0f;
		}
		if (currentPlayer.data.playerActions.Jump.WasPressed)
		{
			currentButton.GetComponent<Button>().onClick.Invoke();
		}
		if (currentPlayer.data.playerActions.Device.Action4.WasPressed)
		{
			currentButton.GetComponent<CharacterCreatorPortrait>().EditCharacter();
		}
		currentlySelectedFace = Mathf.Clamp(currentlySelectedFace, 0, buttons.Length - 1);
	}
}
