using System;

namespace Irc;

public class ChannelMessageEventArgs : EventArgs
{
	public string Channel { get; internal set; }

	public string From { get; internal set; }

	public string Message { get; internal set; }

	public ChannelMessageEventArgs(string Channel, string From, string Message)
	{
		this.Channel = Channel;
		this.From = From;
		this.Message = Message;
	}
}
