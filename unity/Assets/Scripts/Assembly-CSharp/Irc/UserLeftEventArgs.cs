using System;

namespace Irc
{
	public class UserLeftEventArgs : EventArgs
	{
		public string Channel { get; internal set; }

		public string User { get; internal set; }

		public UserLeftEventArgs(string Channel, string User)
		{
			this.Channel = Channel;
			this.User = User;
		}
	}
}
