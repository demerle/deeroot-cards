namespace Photon.Pun.Simple
{
	public interface IProjectileCannon
	{
		PhotonView PhotonView { get; }

		NetObject NetObj { get; }

		IContactTrigger ContactTrigger { get; }

		int ViewID { get; }
	}
}
