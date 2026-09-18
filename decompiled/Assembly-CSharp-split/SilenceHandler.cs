using Photon.Pun;
using UnityEngine;

public class SilenceHandler : MonoBehaviour
{
	[Header("Settings")]
	public CodeAnimation codeAnim;

	private Player player;

	private CharacterData data;

	private void Start()
	{
		player = GetComponent<Player>();
		data = player.data;
	}

	private void Update()
	{
		if (data.silenceTime > 0f)
		{
			data.silenceTime -= TimeHandler.deltaTime;
			if (!data.isSilenced)
			{
				StartSilence();
			}
		}
		else if (data.isSilenced)
		{
			StopSilence();
		}
	}

	private void StartSilence()
	{
		player.data.input.silencedInput = true;
		codeAnim.PlayIn();
		data.isSilenced = true;
	}

	public void StopSilence()
	{
		player.data.input.silencedInput = false;
		if (codeAnim.currentState == CodeAnimationInstance.AnimationUse.In)
		{
			codeAnim.PlayOut();
		}
		data.isSilenced = false;
		data.silenceTime = 0f;
	}

	private void OnDisable()
	{
		codeAnim.transform.localScale = Vector3.zero;
	}

	[PunRPC]
	public void RPCA_AddSilence(float f)
	{
		if (f > data.silenceTime)
		{
			data.silenceTime = f;
		}
		if (!data.isSilenced)
		{
			StartSilence();
		}
	}
}
