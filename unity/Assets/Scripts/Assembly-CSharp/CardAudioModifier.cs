using UnityEngine;

public class CardAudioModifier : MonoBehaviour
{
	public enum StackType
	{
		RTPCValue = 0,
		PostEvent = 1
	}

	public string stackName;

	public StackType stackType;
}
