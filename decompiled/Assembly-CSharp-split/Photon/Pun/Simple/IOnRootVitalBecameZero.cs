namespace Photon.Pun.Simple;

public interface IOnRootVitalBecameZero
{
	void OnRootVitalBecameZero(Vital vital, IVitalsContactReactor causeOfDeath);
}
