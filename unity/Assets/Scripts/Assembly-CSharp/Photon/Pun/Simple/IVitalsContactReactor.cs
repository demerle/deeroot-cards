namespace Photon.Pun.Simple
{
	public interface IVitalsContactReactor : IContactReactor
	{
		VitalNameType VitalNameType { get; }

		bool AllowOverload { get; }

		bool Propagate { get; }

		double DischargeValue(ContactType contactType = ContactType.Undefined);
	}
}
