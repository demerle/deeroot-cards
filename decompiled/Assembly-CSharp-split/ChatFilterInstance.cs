using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChatFilterInstance
{
	public List<string> words = new List<string>();

	public string category;

	[TextArea]
	public string replacement;
}
