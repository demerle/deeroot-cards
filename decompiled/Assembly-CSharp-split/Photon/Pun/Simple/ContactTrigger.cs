using System;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun.Simple.ContactGroups;
using UnityEngine;

namespace Photon.Pun.Simple;

public class ContactTrigger : MonoBehaviour, IContactTrigger, IContactable, IOnPreSimulate, IOnStateChange
{
	[Tooltip("If ITriggeringComponent has multiple colliders, they will all be capable of triggering Enter/Stay/Exit events. Enabling this prevents that, and will suppress multiple calls on the same object.")]
	[SerializeField]
	public bool preventRepeats = true;

	[SerializeField]
	[HideInInspector]
	public int[] _ignoredSystems;

	protected List<Type> ignoredSystems = new List<Type>();

	public List<IOnContactEvent> OnContactEventCallbacks = new List<IOnContactEvent>(1);

	private List<IContactSystem> _contactSystems = new List<IContactSystem>(0);

	[Tooltip("This ContactTrigger can act as a proxy of another. For example projectiles set the proxy as the shooters ContactTrigger, so projectile hits can be treated as hits by the players weapon. Default setting is 'this', indicating this isn't a proxy.")]
	public IContactTrigger _proxy;

	protected NetObject netObj;

	protected ISyncContact syncContact;

	protected IContactGroupsAssign contactGroupsAssign;

	internal ContactType usedContactTypes;

	protected HashSet<IContactSystem> triggeringHitscans = new HashSet<IContactSystem>();

	protected HashSet<IContactSystem> triggeringEnters = new HashSet<IContactSystem>();

	protected HashSet<IContactSystem> triggeringStays = new HashSet<IContactSystem>();

	public static List<IContactSystem> tempFindSystems;

	internal static List<Type> contactSystemTypes;

	public bool PreventRepeats
	{
		get
		{
			return preventRepeats;
		}
		set
		{
			preventRepeats = value;
		}
	}

	public List<IContactSystem> ContactSystems => _contactSystems;

	public IContactTrigger Proxy
	{
		get
		{
			return _proxy;
		}
		set
		{
			_proxy = value;
		}
	}

	public byte Index { get; set; }

	public NetObject NetObj => netObj;

	public ISyncContact SyncContact => syncContact;

	public IContactGroupsAssign ContactGroupsAssign => contactGroupsAssign;

	static ContactTrigger()
	{
		tempFindSystems = new List<IContactSystem>(2);
		contactSystemTypes = new List<Type>();
		FindDerivedTypesFromAssembly();
	}

	public virtual void PollInterfaces()
	{
		_contactSystems.Clear();
		base.transform.GetNestedComponentsInParents<IContactSystem, NetObject>(tempFindSystems);
		int i = 0;
		for (int count = tempFindSystems.Count; i < count; i++)
		{
			Type type = tempFindSystems[i].GetType();
			bool flag = false;
			foreach (Type ignoredSystem in ignoredSystems)
			{
				if (type.CheckIsAssignableFrom(ignoredSystem))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				_contactSystems.Add(tempFindSystems[i]);
			}
		}
		base.transform.GetNestedComponentsInChildren(OnContactEventCallbacks, true, typeof(NetObject), typeof(IContactTrigger));
	}

	public virtual void Awake()
	{
		if (_proxy == null)
		{
			_proxy = this;
		}
		GetAllowedTypesFromHashes();
		PollInterfaces();
		netObj = base.transform.GetParentComponent<NetObject>();
		syncContact = GetComponent<ISyncContact>();
		contactGroupsAssign = GetComponent<ContactGroupAssign>();
		if (contactGroupsAssign == null)
		{
			IContactGroupsAssign nestedComponentInParent = base.transform.GetNestedComponentInParent<IContactGroupsAssign, NetObject>();
			if (nestedComponentInParent != null && nestedComponentInParent.ApplyToChildren)
			{
				contactGroupsAssign = nestedComponentInParent;
			}
		}
		foreach (IOnContactEvent onContactEventCallback in OnContactEventCallbacks)
		{
			usedContactTypes |= onContactEventCallback.TriggerOn;
		}
	}

	protected virtual void OnEnable()
	{
		if (preventRepeats)
		{
			NetMasterCallbacks.RegisterCallbackInterfaces(this);
			triggeringHitscans.Clear();
			triggeringEnters.Clear();
			triggeringStays.Clear();
		}
	}

	protected virtual void OnDisable()
	{
		if (preventRepeats)
		{
			NetMasterCallbacks.RegisterCallbackInterfaces(this, register: false, delay: true);
		}
	}

	public void OnStateChange(ObjState newState, ObjState previousState, Transform attachmentTransform, Mount attachTo = null, bool isReady = true)
	{
		if (preventRepeats)
		{
			triggeringEnters.Clear();
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		Contact(other, ContactType.Enter);
	}

	private void OnTriggerEnter(Collider other)
	{
		Contact(other, ContactType.Enter);
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		Contact(collision.collider, ContactType.Enter);
	}

	private void OnCollisionEnter(Collision collision)
	{
		Contact(collision.collider, ContactType.Enter);
	}

	private void OnTriggerStay2D(Collider2D other)
	{
		Contact(other, ContactType.Stay);
	}

	private void OnTriggerStay(Collider other)
	{
		Contact(other, ContactType.Stay);
	}

	private void OnCollisionStay2D(Collision2D collision)
	{
		Contact(collision.collider, ContactType.Stay);
	}

	private void OnCollisionStay(Collision collision)
	{
		Contact(collision.collider, ContactType.Stay);
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		Contact(other, ContactType.Exit);
	}

	private void OnTriggerExit(Collider other)
	{
		Contact(other, ContactType.Exit);
	}

	private void OnCollisionExit2D(Collision2D collision)
	{
		Contact(collision.collider, ContactType.Exit);
	}

	private void OnCollisionExit(Collision collision)
	{
		Contact(collision.collider, ContactType.Exit);
	}

	protected virtual void Contact(Component otherCollider, ContactType contactType)
	{
		IContactTrigger nestedComponentInParents = otherCollider.transform.GetNestedComponentInParents<IContactTrigger, NetObject>();
		if (nestedComponentInParents != null && !CheckIsNested(this, nestedComponentInParents.Proxy) && (object)_proxy.NetObj != nestedComponentInParents.Proxy.NetObj)
		{
			nestedComponentInParents.Proxy.OnContact(this, contactType);
			_proxy.OnContact(nestedComponentInParents, contactType);
		}
	}

	protected bool CheckIsNested(IContactTrigger first, IContactTrigger second)
	{
		NetObject netObject = first.NetObj;
		NetObject netObject2 = second.NetObj;
		NetObject netObject3 = netObject;
		while ((object)netObject3 != null)
		{
			if ((object)netObject3 == netObject2)
			{
				return true;
			}
			Transform parent = netObject3.transform.parent;
			if ((object)parent == null)
			{
				break;
			}
			netObject3 = parent.GetParentComponent<NetObject>();
		}
		netObject3 = netObject2;
		while ((object)netObject3 != null)
		{
			if ((object)netObject3 == netObject)
			{
				return true;
			}
			Transform parent2 = netObject3.transform.parent;
			if ((object)parent2 == null)
			{
				break;
			}
			netObject3 = parent2.GetParentComponent<NetObject>();
		}
		return false;
	}

	public virtual void OnContact(IContactTrigger otherCT, ContactType contactType)
	{
		if ((bool)GetComponent<ContactProjectile>() && contactType == ContactType.Enter)
		{
			Debug.Log("Prj Contact");
		}
		List<IContactSystem> contactSystems = otherCT.Proxy.ContactSystems;
		int count = contactSystems.Count;
		if (count == 0 || (netObj != null && !_proxy.NetObj.AllObjsAreReady))
		{
			return;
		}
		NetObject netObject = otherCT.Proxy.NetObj;
		if (netObject != null && !netObject.AllObjsAreReady)
		{
			Debug.Log(Time.time + base.name + " " + netObject.photonView.OwnerActorNr + " Other object not ready so ignoring contact");
			return;
		}
		for (int i = 0; i < count; i++)
		{
			IContactSystem contactSystem = contactSystems[i];
			if (!IsCompatibleSystem(contactSystem, otherCT))
			{
				continue;
			}
			if (preventRepeats)
			{
				switch (contactType)
				{
				case ContactType.Enter:
					if (triggeringEnters.Contains(contactSystem))
					{
						continue;
					}
					triggeringEnters.Add(contactSystem);
					break;
				case ContactType.Stay:
					if (triggeringStays.Contains(contactSystem))
					{
						continue;
					}
					triggeringStays.Add(contactSystem);
					break;
				case ContactType.Exit:
					if (!triggeringEnters.Contains(contactSystem))
					{
						continue;
					}
					triggeringEnters.Remove(contactSystem);
					break;
				case ContactType.Hitscan:
					if (triggeringHitscans.Contains(contactSystem))
					{
						continue;
					}
					triggeringHitscans.Add(contactSystem);
					break;
				}
			}
			if ((usedContactTypes & contactType) == 0)
			{
				break;
			}
			ContactEvent contactEvent = new ContactEvent(contactSystem, otherCT, contactType);
			if (Proxy.SyncContact == null)
			{
				ContactCallbacks(contactEvent);
			}
			else
			{
				syncContact.SyncContactEvent(contactEvent);
			}
		}
	}

	public virtual Consumption ContactCallbacks(ContactEvent contactEvent)
	{
		Consumption consumption = Consumption.None;
		int i = 0;
		for (int count = OnContactEventCallbacks.Count; i < count; i++)
		{
			consumption |= OnContactEventCallbacks[i].OnContactEvent(contactEvent);
			if (consumption == Consumption.All)
			{
				return Consumption.All;
			}
		}
		return consumption;
	}

	public void OnPreSimulate(int frameId, int subFrameId)
	{
		if (preventRepeats)
		{
			triggeringHitscans.Clear();
			triggeringStays.Clear();
		}
	}

	internal void GetAllowedTypesFromHashes()
	{
		ignoredSystems.Clear();
		if (_ignoredSystems == null)
		{
			return;
		}
		foreach (Type contactSystemType in contactSystemTypes)
		{
			int hashCode = contactSystemType.Name.GetHashCode();
			bool flag = false;
			int[] array = _ignoredSystems;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == hashCode)
				{
					flag = true;
				}
			}
			if (flag)
			{
				ignoredSystems.Add(contactSystemType);
			}
		}
	}

	internal static void FindDerivedTypesFromAssembly()
	{
		contactSystemTypes.Clear();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			Type[] types = assemblies[i].GetTypes();
			foreach (Type type in types)
			{
				if (!type.IsAbstract && typeof(IContactSystem).CheckIsAssignableFrom(type))
				{
					contactSystemTypes.Add(type);
				}
			}
		}
	}

	private bool IsCompatibleSystem(IContactSystem system, IContactTrigger ct)
	{
		Type type = system.GetType();
		foreach (Type ignoredSystem in ignoredSystems)
		{
			if (ignoredSystem.CheckIsAssignableFrom(type))
			{
				return false;
			}
		}
		return true;
	}
}
