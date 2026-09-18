namespace Photon.Pun.Simple;

public struct ContactEvent
{
	public readonly IContactSystem contactSystem;

	public readonly IContactTrigger contactTrigger;

	public readonly ContactType contactType;

	public ContactEvent(IContactSystem contactSystem, IContactTrigger contacter, ContactType contactType)
	{
		this.contactSystem = contactSystem;
		contactTrigger = contacter;
		this.contactType = contactType;
	}

	public ContactEvent(ContactEvent contactEvent)
	{
		contactSystem = contactEvent.contactSystem;
		contactTrigger = contactEvent.contactTrigger;
		contactType = contactEvent.contactType;
	}
}
