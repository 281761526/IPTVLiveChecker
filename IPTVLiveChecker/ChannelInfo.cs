using System;

namespace IPTVLiveChecker;

internal class ChannelInfo
{
	public string Name { get; set; }

	public string Url { get; set; }

	public string Location { get; set; }

	public string Resolution { get; set; }

	public string Speed { get; set; }

	public string Group { get; set; }

	public string Status { get; set; }

	public bool Visible { get; set; } = true;

	public DateTime ParseDateTime { get; set; } = DateTime.MinValue;
}
