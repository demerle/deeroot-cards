using UnityEngine;

namespace Photon.Pun.Simple
{
	[HelpURL("https://doc.photonengine.com/en-us/pun/current/gameplay/simple/simpleoverview")]
	public abstract class NetComponent : MonoBehaviour, IOnJoinedRoom, IOnAwake, IOnStart, IOnEnable, IOnDisable, IOnAuthorityChanged
	{
		[HideInInspector]
		[SerializeField]
		protected int prefabInstanceId;

		protected NetObject netObj;

		protected PhotonView photonView;

		protected bool hadFirstAuthorityAssgn;

		public NetObject NetObj
		{
			get
			{
				return netObj;
			}
		}

		public RigidbodyType RigidbodyType { get; private set; }

		public int ViewID
		{
			get
			{
				return photonView.ViewID;
			}
		}

		public PhotonView PhotonView
		{
			get
			{
				return photonView;
			}
		}

		public bool IsMine
		{
			get
			{
				return photonView.IsMine;
			}
		}

		public int ControllerActorNr
		{
			get
			{
				return photonView.ControllerActorNr;
			}
		}

		protected virtual void Reset()
		{
		}

		protected virtual void OnValidate()
		{
		}

		public virtual void OnJoinedRoom()
		{
		}

		public void Awake()
		{
			if (!base.transform.GetParentComponent<NetObject>())
			{
				OnAwakeInitialize(false);
			}
		}

		public virtual void OnAwake()
		{
			netObj = base.transform.GetParentComponent<NetObject>();
			EnsureComponentsDependenciesExist();
			OnAwakeInitialize(true);
		}

		public virtual void OnAwakeInitialize(bool isNetObject)
		{
		}

		protected virtual NetObject EnsureComponentsDependenciesExist()
		{
			if (!netObj)
			{
				netObj = base.transform.GetParentComponent<NetObject>();
			}
			if ((bool)netObj)
			{
				photonView = netObj.GetComponent<PhotonView>();
				RigidbodyType = (netObj.Rb ? RigidbodyType.RB : (netObj.Rb2D ? RigidbodyType.RB2D : RigidbodyType.None));
				return netObj;
			}
			Debug.LogError("NetComponent derived class cannot find a NetObject on '" + base.transform.root.name + "'.");
			return null;
		}

		public virtual void Start()
		{
			if (!netObj)
			{
				OnStartInitialize(false);
			}
		}

		public virtual void OnStart()
		{
			OnStartInitialize(true);
		}

		public virtual void OnStartInitialize(bool isNetObject)
		{
		}

		public virtual void OnPostEnable()
		{
		}

		public virtual void OnPostDisable()
		{
			hadFirstAuthorityAssgn = false;
		}

		public virtual void OnAuthorityChanged(bool isMine, bool controllerChanged)
		{
			if (controllerChanged && !hadFirstAuthorityAssgn)
			{
				OnFirstAuthorityAssign(isMine, controllerChanged);
				hadFirstAuthorityAssgn = true;
			}
		}

		public virtual void OnFirstAuthorityAssign(bool isMine, bool asServer)
		{
		}
	}
}
