namespace Photon.Utilities;

public static class BitUtilities
{
	public static int GetBitsForMaxValue(this int maxvalue)
	{
		for (int i = 0; i < 32; i++)
		{
			if (maxvalue >> i == 0)
			{
				return i;
			}
		}
		return 32;
	}

	public static int GetBitsForMaxValue(this uint maxvalue)
	{
		for (int i = 0; i < 32; i++)
		{
			if (maxvalue >> i == 0)
			{
				return i;
			}
		}
		return 32;
	}
}
