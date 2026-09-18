using System.Collections.Generic;

namespace Photon.Pun.Simple
{
	public interface IContactTrigger
	{
		NetObject NetObj { get; }

		byte Index { get; set; }

		IContactTrigger Proxy { get; set; }

		bool PreventRepeats { get; set; }

		List<IContactSystem> ContactSystems { get; }

		ISyncContact SyncContact { get; }

		IContactGroupsAssign ContactGroupsAssign { get; }

		Consumption ContactCallbacks(ContactEvent contactEvent);

		void OnContact(IContactTrigger otherCT, ContactType contactType);
	}
}
