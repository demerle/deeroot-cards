namespace Photon.Pun.Simple
{
	public interface IOnVitalsValueChange : IOnVitalsChange
	{
		void OnVitalValueChange(Vital vital);
	}
}
