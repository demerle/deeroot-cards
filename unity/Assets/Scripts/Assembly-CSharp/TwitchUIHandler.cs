using System;
using Irc;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TwitchUIHandler : MonoBehaviour
{
	private const string TWITCH_OAUTH_PLAYERPREF_KEY = "TwitchOauth";

	private const string TWITCH_NAME_PLAYERPREF_KEY = "TwitchName";

	[SerializeField]
	private TMP_InputField m_UserNameText;

	[SerializeField]
	private TMP_InputField m_OauthText;

	[SerializeField]
	private Button m_GetOAuthButton;

	[SerializeField]
	private ListMenuPage m_TwitchBar;

	private Action<string, string> m_OnMsgAction;

	private bool m_Connected;

	public static string OAUTH_KEY
	{
		get
		{
			return PlayerPrefs.GetString("TwitchOauth" + SteamUser.GetSteamID().ToString(), string.Empty);
		}
		private set
		{
			PlayerPrefs.SetString("TwitchOauth" + SteamUser.GetSteamID().ToString(), value);
		}
	}

	public static string TWITCH_NAME_KEY
	{
		get
		{
			return PlayerPrefs.GetString("TwitchName" + SteamUser.GetSteamID().ToString(), string.Empty);
		}
		private set
		{
			PlayerPrefs.SetString("TwitchName" + SteamUser.GetSteamID().ToString(), value);
		}
	}

	public static TwitchUIHandler Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
		InitListeners();
	}

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
		if (!string.IsNullOrEmpty(OAUTH_KEY))
		{
			m_OauthText.text = OAUTH_KEY;
		}
		if (!string.IsNullOrEmpty(TWITCH_NAME_KEY))
		{
			m_UserNameText.text = TWITCH_NAME_KEY;
		}
	}

	private void InitListeners()
	{
		m_GetOAuthButton.onClick.AddListener(GetOauth);
	}

	public void AddMsgAction(Action<string, string> a)
	{
		m_OnMsgAction = (Action<string, string>)Delegate.Combine(m_OnMsgAction, a);
	}

	public void OnContinueClick()
	{
		OAUTH_KEY = m_OauthText.text;
		TWITCH_NAME_KEY = m_UserNameText.text.ToLower();
		TwitchIrc.Instance.Username = TWITCH_NAME_KEY;
		TwitchIrc.Instance.OauthToken = OAUTH_KEY;
		TwitchIrc.Instance.Channel = TWITCH_NAME_KEY;
		TwitchIrc.Instance.Connect();
	}

	private void GetOauth()
	{
		Application.OpenURL("http://twitchapps.com/tmi/");
	}

	public void MessageSend()
	{
	}

	public void GoUrl(string url)
	{
		Application.OpenURL(url);
	}

	private void OnServerMessage(string message)
	{
		Debug.Log(message);
	}

	private void OnChannelMessage(ChannelMessageEventArgs channelMessageArgs)
	{
		Action<string, string> onMsgAction = m_OnMsgAction;
		if (onMsgAction != null)
		{
			onMsgAction(channelMessageArgs.Message, channelMessageArgs.From);
		}
	}

	private void OnUserJoined(UserJoinedEventArgs userJoinedArgs)
	{
		if (userJoinedArgs.User.ToUpper() == TwitchIrc.Instance.Username.ToUpper())
		{
			Debug.Log("LOCAL USER JOINED!");
			ConnectedToTwitch();
		}
	}

	private void ConnectedToTwitch()
	{
		if (!m_Connected)
		{
			m_Connected = true;
			m_TwitchBar.Open();
		}
	}

	private void OnUserLeft(UserLeftEventArgs userLeftArgs)
	{
	}

	private void OnExceptionThrown(Exception exeption)
	{
		Debug.Log(exeption);
	}
}
