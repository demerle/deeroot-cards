using System.Collections.Generic;

namespace Photon.Pun.Simple.Internal;

public static class CallbackUtilities
{
	public static int RegisterInterface<T>(List<T> callbackList, object c, bool register) where T : class
	{
		if (callbackList == null)
		{
			callbackList = new List<T>();
		}
		if (!(c is T item))
		{
			return callbackList.Count;
		}
		if (register)
		{
			if (!callbackList.Contains(item))
			{
				callbackList.Add(item);
			}
		}
		else if (callbackList.Contains(item))
		{
			callbackList.Remove(item);
		}
		return callbackList.Count;
	}
}
