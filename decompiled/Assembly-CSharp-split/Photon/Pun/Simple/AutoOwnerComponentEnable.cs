using System;
using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.Simple;

public class AutoOwnerComponentEnable : NetComponent, IOnAuthorityChanged
{
	public enum EnableIf
	{
		Ignore,
		Owner,
		Other
	}

	[Serializable]
	public class ComponentToggle
	{
		public Behaviour component;

		public EnableIf enableIfOwned = EnableIf.Owner;
	}

	public bool includeChildren = true;

	public bool includeUnity = true;

	public bool includePhoton;

	public bool includeSimple;

	[HideInInspector]
	[SerializeField]
	private List<ComponentToggle> componentToggles = new List<ComponentToggle>();

	[HideInInspector]
	[SerializeField]
	private List<Behaviour> componentLookup = new List<Behaviour>();

	public override void OnStart()
	{
		base.OnStart();
		SwitchAuth(base.IsMine);
	}

	public override void OnJoinedRoom()
	{
		base.OnJoinedRoom();
		SwitchAuth(base.IsMine);
	}

	public override void OnAuthorityChanged(bool isMine, bool controllerChanged)
	{
		base.OnAuthorityChanged(isMine, controllerChanged);
		SwitchAuth(isMine);
	}

	private void SwitchAuth(bool isMine)
	{
		for (int i = 0; i < componentToggles.Count; i++)
		{
			ComponentToggle componentToggle = componentToggles[i];
			if (componentToggle != null && componentToggle.enableIfOwned != EnableIf.Ignore && componentToggle.component != null)
			{
				componentToggle.component.enabled = ((componentToggle.enableIfOwned == EnableIf.Owner) ? isMine : (!isMine));
			}
		}
	}
}
