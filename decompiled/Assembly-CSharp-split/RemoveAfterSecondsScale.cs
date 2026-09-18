using UnityEngine;

public class RemoveAfterSecondsScale : MonoBehaviour
{
	private void Start()
	{
		GetComponent<RemoveAfterSeconds>().seconds *= base.transform.localScale.x;
	}
}
