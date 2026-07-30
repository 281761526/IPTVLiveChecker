using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace IPTVLiveChecker;

internal static class PlayerLogger
{
	private static readonly object _syncRoot = new object();

	private static StreamWriter _writer;

	private static string _logPath;

	private static int _initThreadId = -1;

	private static bool _initialized;

	private static int _sequence;

	public static string LogPath => _logPath ?? string.Empty;

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}
		try
		{
			string logDir = Path.Combine(Application.StartupPath, "logs");
			if (!Directory.Exists(logDir))
			{
				Directory.CreateDirectory(logDir);
			}
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			_logPath = Path.Combine(logDir, $"player_{timestamp}.log");
			EnforceLogRetention(logDir);
			_initThreadId = Thread.CurrentThread.ManagedThreadId;
			bool append = false;
			_writer = new StreamWriter(_logPath, append, Encoding.UTF8, 65536)
			{
				AutoFlush = true
			};
			_initialized = true;
			Write("SYSTEM", $"日志初始化完成 | PID={Process.GetCurrentProcess().Id} | 线程={_initThreadId} | OS={Environment.OSVersion} | .NET={Environment.Version}");
		}
		catch (Exception ex)
		{
			_logPath = string.Empty;
			System.Diagnostics.Debug.WriteLine($"PlayerLogger init failed: {ex.Message}");
		}
	}

	private const int MaxLogFiles = 10;

	/// <summary>
	/// 日志文件数量上限 10：超出时按文件名（时间缀前缀可字典序比较）从最旧开始删除，
	/// 每创建一个新文件最多删除一个旧文件，保持目录内不超过 10 个。
	/// </summary>
	private static void EnforceLogRetention(string logDir)
	{
		try
		{
			string[] files = Directory.GetFiles(logDir, "player_*.log");
			if (files.Length <= MaxLogFiles)
			{
				return;
			}
			// 文件名形如 player_yyyyMMdd_HHmmss.log，字典序即时间序，最旧在前
			Array.Sort(files);
			int toDelete = files.Length - MaxLogFiles;
			for (int i = 0; i < toDelete; i++)
			{
				try
				{
					File.Delete(files[i]);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	public static void Write(string tag, string message)
	{
		if (!_initialized || _writer == null)
		{
			return;
		}
		int seq = Interlocked.Increment(ref _sequence);
		string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
		int threadId = Thread.CurrentThread.ManagedThreadId;
		StringBuilder sb = new StringBuilder(256);
		sb.Append('[');
		sb.Append(timestamp);
		sb.Append("] [#");
		sb.Append(seq);
		sb.Append("] [T");
		sb.Append(threadId);
		sb.Append("] [");
		sb.Append(tag);
		sb.Append("] ");
		sb.Append(message);
		string line = sb.ToString();
		lock (_syncRoot)
		{
			try
			{
				_writer.WriteLine(line);
			}
			catch
			{
			}
		}
	}

	public static void WriteError(string tag, Exception ex)
	{
		if (ex == null)
		{
			return;
		}
		Write(tag, $"EXCEPTION: {ex.GetType().Name} | {ex.Message}");
		if (ex.InnerException != null)
		{
			Write(tag, $"  INNER: {ex.InnerException.GetType().Name} | {ex.InnerException.Message}");
		}
		if (!string.IsNullOrEmpty(ex.StackTrace))
		{
			string[] lines = ex.StackTrace.Split('\n');
			int count = 0;
			foreach (string ln in lines)
			{
				if (++count > 12)
				{
					Write(tag, "  ... (stack truncated)");
					break;
				}
				Write(tag, $"  STACK: {ln.Trim()}");
			}
		}
	}

	public static void Flush()
	{
		lock (_syncRoot)
		{
			try
			{
				_writer?.Flush();
			}
			catch
			{
			}
		}
	}

	public static void Shutdown()
	{
		lock (_syncRoot)
		{
			try
			{
				if (_writer != null)
				{
					Write("SYSTEM", "日志关闭");
					_writer.Flush();
					_writer.Dispose();
					_writer = null;
				}
			}
			catch
			{
			}
			_initialized = false;
		}
	}
}
