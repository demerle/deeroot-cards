namespace Photon.Pun.Simple;

public static class HitscanTypeExt
{
	public static bool IsCast(this HitscanType hitscanType)
	{
		return hitscanType < HitscanType.OverlapSphere;
	}

	public static bool IsOverlap(this HitscanType hitscanType)
	{
		return hitscanType > HitscanType.BoxCast;
	}

	public static bool UsesRadius(this HitscanType hitscanType)
	{
		if (hitscanType != HitscanType.SphereCast && hitscanType != HitscanType.CapsuleCast && hitscanType != HitscanType.OverlapSphere)
		{
			return hitscanType == HitscanType.OverlapCapsule;
		}
		return true;
	}

	public static bool IsBox(this HitscanType hitscanType)
	{
		if (hitscanType != HitscanType.BoxCast)
		{
			return hitscanType == HitscanType.OverlapBox;
		}
		return true;
	}

	public static bool IsCapsule(this HitscanType hitscanType)
	{
		if (hitscanType != HitscanType.CapsuleCast)
		{
			return hitscanType == HitscanType.OverlapCapsule;
		}
		return true;
	}
}
