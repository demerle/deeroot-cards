namespace Photon.Pun.Simple;

public struct VitalData
{
	private double _value;

	public int ticksUntilRegen;

	public int ticksUntilDecay;

	public double Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = value;
		}
	}

	public VitalData(double value, int ticksUntilDecay, int ticksUntilRegen)
	{
		_value = value;
		this.ticksUntilDecay = ticksUntilDecay;
		this.ticksUntilRegen = ticksUntilRegen;
	}

	public override string ToString()
	{
		return _value + " ticksUntilDecay: " + ticksUntilDecay + " ticksUntilRegen: " + ticksUntilRegen;
	}
}
