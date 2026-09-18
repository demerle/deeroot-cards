using System;
using Irc;
using UnityEngine;
using UnityEngine.UI;

public class TwitchIrcExample : MonoBehaviour
{
	public InputField UsernameText;

	public InputField TokenText;

	public InputField ChannelText;

	public Text ChatText;

	public InputField MessageText;

	private void Start()
	{
		TwitchIrc instance = TwitchIrc.Instance;
		instance.OnChannelMessage = (ChannelMessage)Delegate.Combine(instance.OnChannelMessage, new ChannelMessage(OnChannelMessage));
		TwitchIrc instance2 = TwitchIrc.Instance;
		instance2.OnUserLeft = (UserLeft)Delegate.Combine(instance2.OnUserLeft, new UserLeft(OnUserLeft));
		TwitchIrc instance3 = TwitchIrc.Instance;
		instance3.OnUserJoined = (UserJoined)Delegate.Combine(instance3.OnUserJoined, new UserJoined(OnUserJoined));
		TwitchIrc instance4 = TwitchIrc.Instance;
		instance4.OnServerMessage = (ServerMessage)Delegate.Combine(instance4.OnServerMessage, new ServerMessage(OnServerMessage));
		TwitchIrc instance5 = TwitchIrc.Instance;
		instance5.OnExceptionThrown = (ExceptionThrown)Delegate.Combine(instance5.OnExceptionThrown, new ExceptionThrown(OnExceptionThrown));
	}

	public void Connect()
	{
		TwitchIrc.Instance.Username = UsernameText.text;
		TwitchIrc.Instance.OauthToken = TokenText.text;
		TwitchIrc.Instance.Channel = ChannelText.text;
		TwitchIrc.Instance.Connect();
	}

	public void MessageSend()
	{
		if (!string.IsNullOrEmpty(MessageText.text))
		{
			TwitchIrc.Instance.Message(MessageText.text);
			Text chatText = ChatText;
			chatText.text = chatText.text + "<b>" + TwitchIrc.Instance.Username + "</b>: " + MessageText.text + "\n";
			MessageText.text = "";
		}
	}

	public void GoUrl(string url)
	{
		Application.OpenURL(url);
	}

	private void OnServerMessage(string message)
	{
		Text chatText = ChatText;
		chatText.text = chatText.text + "<b>SERVER:</b> " + message + "\n";
		Debug.Log(message);
	}

	private void OnChannelMessage(ChannelMessageEventArgs channelMessageArgs)
	{
		Text chatText = ChatText;
		chatText.text = chatText.text + "<b>" + channelMessageArgs.From + ":</b> " + channelMessageArgs.Message + "\n";
		Debug.Log("MESSAGE: " + channelMessageArgs.From + ": " + channelMessageArgs.Message);
	}

	private void OnUserJoined(UserJoinedEventArgs userJoinedArgs)
	{
		Text chatText = ChatText;
		chatText.text = chatText.text + "<b>USER JOINED:</b> " + userJoinedArgs.User + "\n";
		Debug.Log("USER JOINED: " + userJoinedArgs.User);
	}

	private void OnUserLeft(UserLeftEventArgs userLeftArgs)
	{
		Text chatText = ChatText;
		chatText.text = chatText.text + "<b>USER JOINED:</b> " + userLeftArgs.User + "\n";
		Debug.Log("USER JOINED: " + userLeftArgs.User);
	}

	private void OnExceptionThrown(Exception exeption)
	{
		Debug.Log(exeption);
	}
}
