using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioModifyers : MonoBehaviour
{
	public List<AudioModifyer> modifyers = new List<AudioModifyer>();

	public static List<CardAudioModifier> activeModifyer = new List<CardAudioModifier>();

	public void AddToStack(CardAudioModifier mod)
	{
		int num = -1;
		for (int i = 0; i < modifyers.Count; i++)
		{
			if (modifyers[i].modifier.stackName == mod.stackName)
			{
				num = i;
				break;
			}
		}
		if (num != -1)
		{
			modifyers[num].stacks++;
			return;
		}
		AudioModifyer audioModifyer = new AudioModifyer();
		audioModifyer.modifier = new CardAudioModifier();
		audioModifyer.modifier.stackName = mod.stackName;
		audioModifyer.modifier.stackType = mod.stackType;
		audioModifyer.stacks = 1;
		activeModifyer.Add(audioModifyer.modifier);
		modifyers.Add(audioModifyer);
	}

	public void SetStacks()
	{
		for (int i = 0; i < activeModifyer.Count; i++)
		{
			_ = activeModifyer[i].stackType;
			_ = 1;
			_ = activeModifyer[i].stackType;
		}
		for (int j = 0; j < modifyers.Count; j++)
		{
			_ = modifyers[j].modifier.stackType;
			_ = 1;
			_ = modifyers[j].modifier.stackType;
		}
	}
}
