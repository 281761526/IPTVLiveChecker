using System.Collections.Generic;

namespace IPTVLiveChecker;

internal class UpdateConfig
{
	public string LatestVersion = "";

	public string DownloadUrl = "";

	public string Md5Checksum = "";

	public int VersionCode;

	public bool IsForceUpdate;

	public List<string> Changelog = new List<string>();
}
