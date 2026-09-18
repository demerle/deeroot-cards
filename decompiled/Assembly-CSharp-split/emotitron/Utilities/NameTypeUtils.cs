namespace emotitron.Utilities;

public static class NameTypeUtils
{
	public static int GetVitalTypeForName(string name, string[] enumNames)
	{
		for (int i = 0; i < enumNames.Length; i++)
		{
			if (name == enumNames[i])
			{
				return i;
			}
		}
		return 1;
	}
}
