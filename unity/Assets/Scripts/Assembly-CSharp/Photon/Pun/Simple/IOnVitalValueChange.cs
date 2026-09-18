namespace Photon.Pun.Simple
{
	public interface IOnVitalValueChange : IOnVitalChange
	{
		void OnVitalValueChange(Vital vital);
	}
}
