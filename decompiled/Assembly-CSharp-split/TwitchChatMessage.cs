using UnityEngine;

public class TwitchChatMessage : MonoBehaviour
{
	private void Start()
	{
		base.transform.SetParent(TwitchAudienceVisualizer.instance.transform);
		base.transform.localScale = Vector3.one;
		GetComponent<RectTransform>().anchoredPosition = new Vector2(Random.Range(-850, 850), Random.Range(-475, 475));
	}
}
