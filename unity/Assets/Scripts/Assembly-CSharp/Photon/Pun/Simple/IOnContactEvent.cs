namespace Photon.Pun.Simple
{
	public interface IOnContactEvent
	{
		ContactType TriggerOn { get; }

		Consumption OnContactEvent(ContactEvent contactEvent);
	}
}
