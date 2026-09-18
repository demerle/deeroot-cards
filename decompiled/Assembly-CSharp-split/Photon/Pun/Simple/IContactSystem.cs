using Photon.Pun.Simple.ContactGroups;

namespace Photon.Pun.Simple;

public interface IContactSystem
{
	NetObject NetObj { get; }

	int ViewID { get; }

	bool IsMine { get; }

	byte SystemIndex { get; set; }

	Mount DefaultMount { get; }

	int ValidMountsMask { get; }

	IContactGroupMask ValidContactGroups { get; }

	Consumption TryTrigger(IContactReactor reactor, ContactEvent contactEvent, int compatibleMounts);

	Mount TryPickup(IContactReactor reactor, ContactEvent contactEvent);
}
