using UnityEngine;

namespace Photon.Pun.Simple
{
	public class OnStateChangeToggle : NetComponent, IOnStateChange
	{
		[HideInInspector]
		[Tooltip("How this object should be toggled. GameObject toggles gameObject.SetActive(), Renderer toggles renderer.enabled, and Component toggles component.enabled.")]
		public DisplayToggle toggle;

		[Tooltip("User specified component to toggle enabled.")]
		[HideInInspector]
		public Component component;

		[HideInInspector]
		public GameObject _gameObject;

		[HideInInspector]
		public Renderer _renderer;

		[HideInInspector]
		public ObjStateLogic stateLogic = new ObjStateLogic();

		private bool reactToAttached;

		private MonoBehaviour monob;

		private bool show;

		public override void OnAwake()
		{
			base.OnAwake();
			if (toggle == DisplayToggle.Renderer)
			{
				if (_renderer == null)
				{
					_renderer = GetComponent<Renderer>();
				}
			}
			else if (toggle == DisplayToggle.Component)
			{
				monob = component as MonoBehaviour;
			}
			else if (_gameObject == null)
			{
				_gameObject = base.gameObject;
			}
			stateLogic.RecalculateMasks();
			reactToAttached = (stateLogic.notMask & 2) == 0 && (stateLogic.stateMask & 2) != 0;
		}

		public void OnStateChange(ObjState newState, ObjState previousState, Transform pickup, Mount attachedTo = null, bool isReady = true)
		{
			if (!isReady)
			{
				show = false;
			}
			else if (stateLogic.Evaluate((int)newState))
			{
				show = true;
				if (reactToAttached && attachedTo == null && (newState & ObjState.Mounted) != 0)
				{
					show = false;
				}
			}
			else
			{
				show = false;
			}
			DeferredEnable();
		}

		private void DeferredEnable()
		{
			switch (toggle)
			{
			case DisplayToggle.GameObject:
				_gameObject.SetActive(show);
				break;
			case DisplayToggle.Component:
				if ((bool)monob)
				{
					monob.enabled = show;
				}
				break;
			case DisplayToggle.Renderer:
				if ((bool)_renderer)
				{
					_renderer.enabled = show;
				}
				break;
			}
		}
	}
}
