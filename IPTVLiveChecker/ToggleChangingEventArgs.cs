using System;

namespace IPTVLiveChecker;

public class ToggleChangingEventArgs : EventArgs
{
	public bool NewValue { get; }

	public bool Cancel { get; set; }

	public ToggleChangingEventArgs(bool newValue)
	{
		NewValue = newValue;
	}
}
