using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace IPTVLiveChecker;

public static class AnimationSettings
{
	private static readonly string FilePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) ?? ".", "ui.animation.json");

	public static bool ReduceMotion { get; set; }

	public static bool HighContrast { get; set; }

	public static void Load()
	{
		try
		{
			if (!File.Exists(FilePath))
			{
				return;
			}
			Dictionary<string, object> json = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(FilePath));
			if (json != null)
			{
				if (json.TryGetValue("ReduceMotion", out var rm) && rm is bool rb)
				{
					ReduceMotion = rb;
				}
				if (json.TryGetValue("HighContrast", out var hc) && hc is bool hb)
				{
					HighContrast = hb;
				}
			}
		}
		catch
		{
		}
	}

	public static void Save()
	{
		try
		{
			Dictionary<string, object> data = new Dictionary<string, object>
			{
				["ReduceMotion"] = ReduceMotion,
				["HighContrast"] = HighContrast
			};
			File.WriteAllText(FilePath, new JavaScriptSerializer().Serialize(data));
		}
		catch
		{
		}
	}
}
