using System;
using System.Collections.Generic;
using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple.ContactGroups;

public class ContactGroupSettings : SettingsScriptableObject<ContactGroupSettings>
{
	public static bool initialized;

	public const string DEF_NAME = "Default";

	[HideInInspector]
	public List<string> contactGroupTags = new List<string>(2) { "Default", "Critical" };

	public Dictionary<string, int> rewindLayerTagToId = new Dictionary<string, int>();

	[NonSerialized]
	public static int bitsForMask;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Bootstrap()
	{
		_ = SettingsScriptableObject<ContactGroupSettings>.Single;
	}

	public override void Initialize()
	{
		SettingsScriptableObject<ContactGroupSettings>.single = this;
		base.Initialize();
		if (initialized)
		{
			return;
		}
		initialized = true;
		bitsForMask = contactGroupTags.Count - 1;
		for (int i = 0; i < contactGroupTags.Count; i++)
		{
			if (rewindLayerTagToId.ContainsKey(contactGroupTags[i]))
			{
				Debug.LogError("The tag '" + contactGroupTags[i] + "' is used more than once in '" + GetType().Name + "'. Repeats will be discarded, which will likely break some parts of rewind until they are removed.");
			}
			else
			{
				rewindLayerTagToId.Add(contactGroupTags[i], i);
			}
		}
	}

	[Obsolete("Left over from NST, likely not useful any more.")]
	public static int FindClosestMatch(string n, int id)
	{
		ContactGroupSettings contactGroupSettings = SettingsScriptableObject<ContactGroupSettings>.Single;
		if (contactGroupSettings.contactGroupTags.Contains(n))
		{
			return contactGroupSettings.contactGroupTags.IndexOf(n);
		}
		if (id < contactGroupSettings.contactGroupTags.Count)
		{
			return id;
		}
		return 0;
	}
}
