using InControl;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCreatorNavigation : MonoBehaviour
{
	private int itemsInRow = 7;

	private CharacterCreator creator;

	private Transform grid;

	private Transform bar;

	private GameObject currentObject;

	private GameObject currentBarObject;

	private CharacterCreatorDragging dragging;

	public int x;

	public int y;

	public int barPos;

	private float cd;

	private void Awake()
	{
		creator = GetComponent<CharacterCreator>();
		dragging = GetComponentInChildren<CharacterCreatorDragging>();
		grid = base.transform.GetChild(0).GetChild(0);
		bar = base.transform.GetChild(0).GetChild(1);
	}

	private void Update()
	{
		if (creator.playerActions != null)
		{
			PlayerActionUpdate(creator.playerActions.Device);
		}
		else if (creator.playerActions == null)
		{
			creator.currentControl = MenuControllerHandler.menuControl;
			for (int i = 0; i < InputManager.ActiveDevices.Count; i++)
			{
				InputDevice device = InputManager.ActiveDevices[i];
				PlayerActionUpdate(device);
			}
		}
	}

	private void PlayerActionUpdate(InputDevice device)
	{
		cd += Time.unscaledDeltaTime;
		if (device != null)
		{
			if (device.CommandWasPressed)
			{
				creator.Finish();
			}
			dragging.rightStick = device.RightStick.Value;
			GridMovement(device);
			BarMovement(device);
		}
	}

	private void BarMovement(InputDevice device)
	{
		if (!currentBarObject)
		{
			MoveNav(0);
		}
		if (device.LeftTrigger.WasPressed)
		{
			MoveNav(-1);
		}
		if (device.RightTrigger.WasPressed)
		{
			MoveNav(1);
		}
	}

	private void MoveNav(int delta)
	{
		cd = 0f;
		if (delta > 0)
		{
			barPos++;
		}
		if (delta < 0)
		{
			barPos--;
		}
		VerifyBarPos();
	}

	private void VerifyBarPos()
	{
		if ((bool)currentBarObject)
		{
			currentBarObject.GetComponent<HoverEvent>().exitEvent.Invoke();
		}
		barPos = Mathf.Clamp(barPos, 0, 3);
		currentBarObject = bar.GetChild(barPos).gameObject;
		currentBarObject.GetComponent<HoverEvent>().enterEvent.Invoke();
		currentBarObject.GetComponent<Button>().onClick.Invoke();
	}

	private void GridMovement(InputDevice device)
	{
		if (!currentObject)
		{
			MoveNav(Vector2.zero);
		}
		if (device.LeftStick.Value.magnitude > 0.75f && cd > 0.1f)
		{
			MoveNav(device.LeftStick);
		}
		if (device.Action1.WasPressed)
		{
			currentObject.GetComponent<Button>().onClick.Invoke();
		}
	}

	private void MoveNav(Vector2 delta)
	{
		cd = 0f;
		if (delta.x > 0.5f)
		{
			x++;
		}
		if (delta.x < -0.5f)
		{
			x--;
		}
		if (delta.y > 0.5f)
		{
			y--;
		}
		if (delta.y < -0.5f)
		{
			y++;
		}
		VerifyPos();
	}

	private void VerifyPos()
	{
		if ((bool)currentObject)
		{
			currentObject.GetComponent<HoverEvent>().exitEvent.Invoke();
		}
		x = Mathf.Clamp(x, 1, itemsInRow);
		y = Mathf.Clamp(y, 0, Mathf.CeilToInt((grid.childCount - 2) / itemsInRow));
		x = Mathf.Clamp(x, 1, grid.childCount - y * itemsInRow - 1);
		int index = x + y * itemsInRow;
		currentObject = grid.GetChild(index).gameObject;
		currentObject.GetComponent<HoverEvent>().enterEvent.Invoke();
	}
}
