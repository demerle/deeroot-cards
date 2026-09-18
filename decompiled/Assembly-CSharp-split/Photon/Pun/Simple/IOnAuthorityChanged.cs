namespace Photon.Pun.Simple;

public interface IOnAuthorityChanged
{
	void OnAuthorityChanged(bool isMine, bool asServer);
}
