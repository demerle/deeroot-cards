using System;

[Serializable]
public class CardInfoStat
{
	public enum SimpleAmount
	{
		notAssigned,
		aLittleBitOf,
		Some,
		aLotOf,
		aHugeAmountOf,
		slightlyLower,
		lower,
		aLotLower,
		slightlySmaller,
		smaller
	}

	public bool positive;

	public string amount;

	public SimpleAmount simepleAmount;

	public string stat;

	public string GetSimpleAmount()
	{
		string result = "";
		if (simepleAmount == SimpleAmount.aLittleBitOf)
		{
			result = "Slightly more ";
		}
		if (simepleAmount == SimpleAmount.Some)
		{
			result = "More ";
		}
		if (simepleAmount == SimpleAmount.aLotOf)
		{
			result = "A bunch more ";
		}
		if (simepleAmount == SimpleAmount.aHugeAmountOf)
		{
			result = "A huge amount of";
		}
		if (simepleAmount == SimpleAmount.slightlyLower)
		{
			result = "Slightly lower ";
		}
		if (simepleAmount == SimpleAmount.lower)
		{
			result = "Lower ";
		}
		if (simepleAmount == SimpleAmount.aLotLower)
		{
			result = "A lot lower";
		}
		if (simepleAmount == SimpleAmount.smaller)
		{
			result = "Smaller";
		}
		if (simepleAmount == SimpleAmount.slightlySmaller)
		{
			result = "Slightly smaller";
		}
		return result;
	}
}
