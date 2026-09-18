namespace Photon.Pun.Simple
{
	public interface IOnVitalsParamChange : IOnVitalsChange
	{
		void OnVitalParamChange(Vital vital);
	}
}
