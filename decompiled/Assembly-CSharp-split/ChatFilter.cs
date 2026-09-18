using Sirenix.OdinInspector;
using UnityEngine;

public class ChatFilter : MonoBehaviour
{
	[TextArea]
	public string enterData;

	public string category;

	public static ChatFilter instance;

	public ChatFilterInstance[] filters;

	private void Awake()
	{
		instance = this;
	}

	[Button]
	private void EnterData()
	{
		string[] array = enterData.Replace('"', ' ').Trim().Split(',');
		ChatFilterInstance chatFilterInstance = null;
		for (int i = 0; i < filters.Length; i++)
		{
			if (filters[i].category == category)
			{
				chatFilterInstance = filters[i];
				break;
			}
		}
		if (chatFilterInstance == null)
		{
			UnityEngine.Debug.LogError("No valid category target!");
			return;
		}
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = array[j].Trim();
			array[j] = array[j].ToUpper();
			if (array[j] == "")
			{
				continue;
			}
			bool flag = false;
			for (int k = 0; k < chatFilterInstance.words.Count; k++)
			{
				if (chatFilterInstance.words[k] == array[j])
				{
					flag = true;
				}
			}
			if (!flag)
			{
				chatFilterInstance.words.Add(array[j]);
			}
		}
	}

	public string FilterMessage(string message)
	{
		for (int i = 0; i < filters.Length; i++)
		{
			for (int j = 0; j < filters[i].words.Count; j++)
			{
				if (message.ToUpper().Contains(filters[i].words[j]))
				{
					return filters[i].replacement;
				}
			}
		}
		return message;
	}
}
