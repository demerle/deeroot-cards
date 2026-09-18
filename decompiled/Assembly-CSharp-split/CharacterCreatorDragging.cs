using InControl;
using UnityEngine;

public class CharacterCreatorDragging : MonoBehaviour
{
	public Vector2 rightStick;

	private CharacterCreator creator;

	private Transform draggedObject;

	private Vector2 lastMouse = Vector3.zero;

	private void Start()
	{
		creator = GetComponentInParent<CharacterCreator>();
	}

	private void Update()
	{
		if (creator.inputType == GeneralInput.InputType.Keyboard || creator.inputType == GeneralInput.InputType.Either)
		{
			DoMouse();
		}
		if (creator.inputType == GeneralInput.InputType.Controller || creator.inputType == GeneralInput.InputType.Either)
		{
			DoController();
		}
	}

	private void DoController()
	{
		int num = -1;
		int barPos = creator.nav.barPos;
		for (int i = 0; i < base.transform.childCount; i++)
		{
			CharacterItem component = base.transform.GetChild(i).GetComponent<CharacterItem>();
			if (barPos == 0 && component.itemType == CharacterItemType.Eyes)
			{
				num = i;
			}
			if (barPos == 1 && component.itemType == CharacterItemType.Mouth)
			{
				num = i;
			}
			if (barPos == 2 && component.itemType == CharacterItemType.Detail && component.slotNr == 0)
			{
				num = i;
			}
			if (barPos == 3 && component.itemType == CharacterItemType.Detail && component.slotNr == 1)
			{
				num = i;
			}
		}
		if (num >= base.transform.childCount)
		{
			return;
		}
		Vector2 vector = Vector2.zero;
		if (creator.playerActions != null)
		{
			vector = creator.playerActions.Aim.Value;
		}
		else
		{
			for (int j = 0; j < InputManager.ActiveDevices.Count; j++)
			{
				vector = InputManager.ActiveDevices[j].RightStick.Value;
			}
		}
		base.transform.GetChild(num).transform.position += (Vector3)vector * Time.deltaTime * 3f;
		if (vector != Vector2.zero)
		{
			CharacterItem component2 = base.transform.GetChild(num).GetComponent<CharacterItem>();
			Vector2 offset = (Vector2)base.transform.GetChild(num).localPosition - component2.offset;
			creator.SetOffset(offset, component2.itemType, component2.slotNr);
		}
	}

	private void DoMouse()
	{
		Vector2 vector = MainCam.instance.cam.ScreenToWorldPoint(Input.mousePosition);
		if (Input.GetKeyDown(KeyCode.Mouse0))
		{
			float num = 2f;
			int num2 = -1;
			for (int i = 0; i < base.transform.childCount; i++)
			{
				float num3 = Vector2.Distance(vector, base.transform.GetChild(i).transform.position);
				Debug.DrawLine(vector, base.transform.GetChild(i).GetComponent<SpriteRenderer>().bounds.center, Color.red, 2f);
				if (num3 < num)
				{
					num = num3;
					num2 = i;
				}
			}
			if (num2 != -1)
			{
				draggedObject = base.transform.GetChild(num2);
			}
		}
		if (Input.GetKeyUp(KeyCode.Mouse0) && (bool)draggedObject)
		{
			CharacterItem component = draggedObject.GetComponent<CharacterItem>();
			Vector2 offset = (Vector2)draggedObject.transform.localPosition - component.offset;
			creator.SetOffset(offset, component.itemType, component.slotNr);
			draggedObject = null;
		}
		if ((bool)draggedObject)
		{
			draggedObject.transform.position += (Vector3)(vector - lastMouse);
			draggedObject.transform.localPosition = Vector2.ClampMagnitude(draggedObject.transform.localPosition, 4.5f);
		}
		lastMouse = vector;
	}
}
