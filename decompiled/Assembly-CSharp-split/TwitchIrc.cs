using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Irc;
using UnityEngine;

public class TwitchIrc : MonoBehaviour
{
	public UserJoined OnUserJoined;

	public UserLeft OnUserLeft;

	public ChannelMessage OnChannelMessage;

	public ServerMessage OnServerMessage;

	public Connected OnConnected;

	public ExceptionThrown OnExceptionThrown;

	private const string ServerName = "irc.twitch.tv";

	private const int ServerPort = 6667;

	public static TwitchIrc Instance;

	public bool ConnectOnAwake;

	public string Username;

	public string OauthToken;

	public string Channel;

	private TcpClient irc;

	private NetworkStream stream;

	private string inputLine;

	private StreamReader reader;

	private StreamWriter writer;

	public void Connect()
	{
		if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(OauthToken))
		{
			return;
		}
		try
		{
			irc = new TcpClient("irc.twitch.tv", 6667);
			stream = irc.GetStream();
			reader = new StreamReader(stream);
			writer = new StreamWriter(stream);
			Send("USER " + Username + "tmi twitch :" + Username);
			Send("PASS " + OauthToken);
			Send("NICK " + Username);
			StartCoroutine("Listen");
		}
		catch (Exception exeption)
		{
			if (OnExceptionThrown != null)
			{
				OnExceptionThrown(exeption);
			}
		}
	}

	public void Disconnect()
	{
		irc = null;
		StopCoroutine("Listen");
		if (stream != null)
		{
			stream.Dispose();
		}
		if (writer != null)
		{
			writer.Dispose();
		}
		if (reader != null)
		{
			reader.Dispose();
		}
	}

	public void JoinChannel()
	{
		if (!string.IsNullOrEmpty(Channel))
		{
			if (Channel[0] != '#')
			{
				Channel = "#" + Channel;
			}
			if (irc != null && irc.Connected)
			{
				Send("JOIN " + Channel);
			}
		}
	}

	public void LeaveChannel()
	{
		Send("PART " + Channel);
	}

	public void Message(string message)
	{
		Send("PRIVMSG " + Channel + " :" + message);
	}

	private IEnumerator Listen()
	{
		while (true)
		{
			if (stream.DataAvailable && (inputLine = reader.ReadLine()) != null)
			{
				ParseData(inputLine);
			}
			yield return null;
		}
	}

	private void ParseData(string data)
	{
		string[] array = data.Split(' ');
		if (data.Length > 4 && data.Substring(0, 4) == "PING")
		{
			Send("PONG " + array[1]);
			return;
		}
		switch (array[1])
		{
		case "001":
			Send("MODE " + Username + " +B");
			OnConnected();
			break;
		case "JOIN":
			if (Instance.OnUserJoined != null)
			{
				Instance.OnUserJoined(new UserJoinedEventArgs(array[2], array[0].Substring(1, array[0].IndexOf("!") - 1)));
			}
			break;
		case "PRIVMSG":
			if (array[2].ToLower() != Username.ToLower() && OnChannelMessage != null)
			{
				OnChannelMessage(new ChannelMessageEventArgs(array[2], array[0].Substring(1, array[0].IndexOf('!') - 1), JoinArray(array, 3)));
			}
			break;
		case "PART":
		case "QUIT":
			if (OnUserLeft != null)
			{
				OnUserLeft(new UserLeftEventArgs(array[2], array[0].Substring(1, data.IndexOf("!") - 1)));
			}
			break;
		default:
			if (array.Length > 3 && OnServerMessage != null)
			{
				OnServerMessage(JoinArray(array, 3));
			}
			break;
		}
	}

	private string StripMessage(string message)
	{
		foreach (Match item in new Regex("\u0003(?:\\d{1,2}(?:,\\d{1,2})?)?").Matches(message))
		{
			message = message.Replace(item.Value, "");
		}
		if (message == "")
		{
			return "";
		}
		if (message.Substring(0, 1) == ":" && message.Length > 2)
		{
			return message.Substring(1, message.Length - 1);
		}
		return message;
	}

	private string JoinArray(string[] strArray, int startIndex)
	{
		return StripMessage(string.Join(" ", strArray, startIndex, strArray.Length - startIndex));
	}

	private void Send(string message)
	{
		writer.WriteLine(message);
		writer.Flush();
	}

	private void OnConnectedToServer()
	{
		JoinChannel();
	}

	private void Awake()
	{
		Instance = this;
		OnConnected = (Connected)Delegate.Combine(OnConnected, new Connected(OnConnectedToServer));
		if (ConnectOnAwake)
		{
			Connect();
		}
	}

	private void OnDisable()
	{
		Disconnect();
	}
}
