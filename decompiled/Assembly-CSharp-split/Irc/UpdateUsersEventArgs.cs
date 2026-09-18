using System;

namespace Irc;

public class UpdateUsersEventArgs : EventArgs
{
	public string Channel { get; internal set; }

	public string[] UserList { get; internal set; }

	public UpdateUsersEventArgs(string Channel, string[] UserList)
	{
		this.Channel = Channel;
		this.UserList = UserList;
	}
}
