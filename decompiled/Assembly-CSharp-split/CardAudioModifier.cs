using UnityEngine;

public class CardAudioModifier : MonoBehaviour
{
	public enum StackType
	{
		RTPCValue,
		PostEvent
	}

	public string stackName;

	public StackType stackType;
}
