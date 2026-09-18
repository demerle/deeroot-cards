using System.Collections.Generic;

namespace Photon.Pun.Simple.Internal
{
	public static class CallbackUtilities
	{
		public static int RegisterInterface<T>(List<T> callbackList, object c, bool register) where T : class
		{
			if (callbackList == null)
			{
				callbackList = new List<T>();
			}
			T val = c as T;
			if (val == null)
			{
				return callbackList.Count;
			}
			if (register)
			{
				if (!callbackList.Contains(val))
				{
					callbackList.Add(val);
				}
			}
			else if (callbackList.Contains(val))
			{
				callbackList.Remove(val);
			}
			return callbackList.Count;
		}
	}
}
