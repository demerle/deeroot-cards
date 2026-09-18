using System;
using InControl;

public class PlayerActions : PlayerActionSet
{
	public PlayerAction Fire;

	public PlayerAction Block;

	public PlayerAction Jump;

	public PlayerAction Left;

	public PlayerAction Right;

	public PlayerAction Up;

	public PlayerAction Down;

	public PlayerTwoAxisAction Move;

	public PlayerTwoAxisAction Aim;

	public PlayerAction AimLeft;

	public PlayerAction AimRight;

	public PlayerAction AimUp;

	public PlayerAction AimDown;

	public PlayerAction Start;

	public PlayerActions()
	{
		Fire = CreatePlayerAction("Fire");
		Start = CreatePlayerAction("Start");
		Block = CreatePlayerAction("Block");
		Jump = CreatePlayerAction("Jump");
		Left = CreatePlayerAction("Move Left");
		Right = CreatePlayerAction("Move Right");
		Up = CreatePlayerAction("Move Up");
		Down = CreatePlayerAction("Move Down");
		AimLeft = CreatePlayerAction("Aim Left");
		AimRight = CreatePlayerAction("Aim Right");
		AimUp = CreatePlayerAction("Aim Up");
		AimDown = CreatePlayerAction("Aim Down");
		Move = CreateTwoAxisPlayerAction(Left, Right, Down, Up);
		Aim = CreateTwoAxisPlayerAction(AimLeft, AimRight, AimDown, AimUp);
	}

	public static PlayerActions CreateWithKeyboardBindings()
	{
		PlayerActions playerActions = new PlayerActions();
		playerActions.Fire.AddDefaultBinding(Mouse.LeftButton);
		playerActions.Block.AddDefaultBinding(Mouse.RightButton);
		playerActions.Jump.AddDefaultBinding(Key.Space);
		playerActions.Up.AddDefaultBinding(Key.W);
		playerActions.Down.AddDefaultBinding(Key.S);
		playerActions.Left.AddDefaultBinding(Key.A);
		playerActions.Right.AddDefaultBinding(Key.D);
		playerActions.Start.AddDefaultBinding(Key.Return);
		playerActions.ListenOptions.IncludeUnknownControllers = true;
		playerActions.ListenOptions.MaxAllowedBindings = 4u;
		playerActions.ListenOptions.UnsetDuplicateBindingsOnSet = true;
		playerActions.ListenOptions.OnBindingFound = delegate(PlayerAction action, BindingSource binding)
		{
			if (binding == new KeyBindingSource(Key.Escape))
			{
				action.StopListeningForBinding();
				return false;
			}
			return true;
		};
		BindingListenOptions bindingListenOptions = playerActions.ListenOptions;
		bindingListenOptions.OnBindingAdded = (Action<PlayerAction, BindingSource>)Delegate.Combine(bindingListenOptions.OnBindingAdded, (Action<PlayerAction, BindingSource>)delegate(PlayerAction action, BindingSource binding)
		{
			Debug.Log("Binding added... " + binding.DeviceName + ": " + binding.Name);
		});
		BindingListenOptions bindingListenOptions2 = playerActions.ListenOptions;
		bindingListenOptions2.OnBindingRejected = (Action<PlayerAction, BindingSource, BindingSourceRejectionType>)Delegate.Combine(bindingListenOptions2.OnBindingRejected, (Action<PlayerAction, BindingSource, BindingSourceRejectionType>)delegate(PlayerAction action, BindingSource binding, BindingSourceRejectionType reason)
		{
			Debug.Log("Binding rejected... " + reason);
		});
		return playerActions;
	}

	public static PlayerActions CreateWithControllerBindings()
	{
		PlayerActions playerActions = new PlayerActions();
		playerActions.Fire.AddDefaultBinding(InputControlType.Action3);
		playerActions.Fire.AddDefaultBinding(InputControlType.RightTrigger);
		playerActions.Block.AddDefaultBinding(InputControlType.Action2);
		playerActions.Block.AddDefaultBinding(InputControlType.LeftTrigger);
		playerActions.Jump.AddDefaultBinding(InputControlType.Action1);
		playerActions.Jump.AddDefaultBinding(InputControlType.LeftBumper);
		playerActions.Jump.AddDefaultBinding(InputControlType.RightBumper);
		playerActions.Start.AddDefaultBinding(InputControlType.Start);
		playerActions.Left.AddDefaultBinding(InputControlType.LeftStickLeft);
		playerActions.Right.AddDefaultBinding(InputControlType.LeftStickRight);
		playerActions.Up.AddDefaultBinding(InputControlType.LeftStickUp);
		playerActions.Down.AddDefaultBinding(InputControlType.LeftStickDown);
		playerActions.AimLeft.AddDefaultBinding(InputControlType.RightStickLeft);
		playerActions.AimRight.AddDefaultBinding(InputControlType.RightStickRight);
		playerActions.AimUp.AddDefaultBinding(InputControlType.RightStickUp);
		playerActions.AimDown.AddDefaultBinding(InputControlType.RightStickDown);
		playerActions.ListenOptions.IncludeUnknownControllers = true;
		playerActions.ListenOptions.MaxAllowedBindings = 4u;
		playerActions.ListenOptions.UnsetDuplicateBindingsOnSet = true;
		playerActions.ListenOptions.OnBindingFound = delegate(PlayerAction action, BindingSource binding)
		{
			if (binding == new KeyBindingSource(Key.Escape))
			{
				action.StopListeningForBinding();
				return false;
			}
			return true;
		};
		BindingListenOptions bindingListenOptions = playerActions.ListenOptions;
		bindingListenOptions.OnBindingAdded = (Action<PlayerAction, BindingSource>)Delegate.Combine(bindingListenOptions.OnBindingAdded, (Action<PlayerAction, BindingSource>)delegate(PlayerAction action, BindingSource binding)
		{
			Debug.Log("Binding added... " + binding.DeviceName + ": " + binding.Name);
		});
		BindingListenOptions bindingListenOptions2 = playerActions.ListenOptions;
		bindingListenOptions2.OnBindingRejected = (Action<PlayerAction, BindingSource, BindingSourceRejectionType>)Delegate.Combine(bindingListenOptions2.OnBindingRejected, (Action<PlayerAction, BindingSource, BindingSourceRejectionType>)delegate(PlayerAction action, BindingSource binding, BindingSourceRejectionType reason)
		{
			Debug.Log("Binding rejected... " + reason);
		});
		return playerActions;
	}
}
