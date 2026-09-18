using System;

namespace Irc;

public class StringEventArgs : EventArgs
{
	public string Result { get; internal set; }

	public StringEventArgs(string s)
	{
		Result = s;
	}

	public override string ToString()
	{
		return Result;
	}
}
