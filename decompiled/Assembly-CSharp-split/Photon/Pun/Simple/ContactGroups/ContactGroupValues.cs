using System;
using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.Simple.ContactGroups;

[Serializable]
public class ContactGroupValues
{
	[SerializeField]
	public List<float> values = new List<float> { 1f };
}
