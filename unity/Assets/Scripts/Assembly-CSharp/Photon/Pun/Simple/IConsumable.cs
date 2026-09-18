namespace Photon.Pun.Simple
{
	public interface IConsumable
	{
		double Charges { get; set; }

		Consumption Consumption { get; }

		ConsumedDespawn ConsumedDespawn { get; }
	}
}
