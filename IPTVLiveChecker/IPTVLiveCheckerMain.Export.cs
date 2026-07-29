using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IPTVLiveChecker;

public partial class IPTVLiveCheckerMain
{
	private void ExportToM3u(string filePath)
	{
		try
		{
			using (StreamWriter sw = new StreamWriter(filePath, append: false, Encoding.UTF8))
			{
				sw.WriteLine("#EXTM3U");
				foreach (ChannelInfo ch in allChannels)
				{
					string cleanName = ChannelLogoHelper.CleanChannelName(ch.Name);
					string logo = ChannelLogoHelper.ResolveLogo(ch.Name);
					sw.WriteLine("#EXTINF:-1 tvg-name=\"" + cleanName + "\" tvg-logo=\"" + logo + "\" group-title=\"" + ch.Group + "\"," + cleanName);
					sw.WriteLine(ch.Url);
				}
			}
			DarkMessageBox.Show($"成功导出 {allChannels.Count} 条数据", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("导出失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void ExportToTxtMergeUrl(string filePath)
	{
		try
		{
			Dictionary<string, List<string>> merged = new Dictionary<string, List<string>>();
			Dictionary<string, string> groupMap = new Dictionary<string, string>();
			foreach (ChannelInfo ch in allChannels)
			{
				string n = ChannelLogoHelper.CleanChannelName(ch.Name);
				if (!string.IsNullOrWhiteSpace(n) && !string.IsNullOrWhiteSpace(ch.Url))
				{
					if (!merged.ContainsKey(n))
					{
						merged[n] = new List<string>();
						groupMap[n] = ch.Group ?? "";
					}
					if (!merged[n].Contains(ch.Url))
					{
						merged[n].Add(ch.Url);
					}
				}
			}
			using (StreamWriter sw = new StreamWriter(filePath, append: false, Encoding.UTF8))
			{
				foreach (KeyValuePair<string, List<string>> kv in merged)
				{
					string urls = string.Join("#", kv.Value);
					sw.WriteLine(kv.Key + "," + urls);
				}
			}
			DarkMessageBox.Show($"成功导出 {merged.Count} 条数据（已合并相同频道，共 {allChannels.Count} 个源）", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("导出失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void ExportToM3uMergeGroup(string filePath)
	{
		try
		{
			Dictionary<string, List<ChannelInfo>> merged = new Dictionary<string, List<ChannelInfo>>();
			foreach (ChannelInfo ch in allChannels)
			{
				string n = ChannelLogoHelper.CleanChannelName(ch.Name);
				if (!string.IsNullOrWhiteSpace(n) && !string.IsNullOrWhiteSpace(ch.Url))
				{
					if (!merged.ContainsKey(n))
					{
						merged[n] = new List<ChannelInfo>();
					}
					merged[n].Add(ch);
				}
			}
			using (StreamWriter sw = new StreamWriter(filePath, append: false, Encoding.UTF8))
			{
				sw.WriteLine("#EXTM3U");
				foreach (KeyValuePair<string, List<ChannelInfo>> kv in merged)
				{
					string group = kv.Value.FirstOrDefault()?.Group ?? "";
					foreach (ChannelInfo ch2 in kv.Value)
					{
						string cleanName = ChannelLogoHelper.CleanChannelName(ch2.Name);
						string logo = ChannelLogoHelper.ResolveLogo(ch2.Name);
						sw.WriteLine("#EXTINF:-1 tvg-name=\"" + cleanName + "\" tvg-logo=\"" + logo + "\" group-title=\"" + group + "\"," + cleanName);
						sw.WriteLine(ch2.Url);
					}
				}
			}
			DarkMessageBox.Show($"成功导出 {allChannels.Count} 条数据（{merged.Count} 个频道，按名称分组）", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("导出失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void ExportToTxt(string filePath, bool merge)
	{
		try
		{
			using StreamWriter sw = new StreamWriter(filePath, append: false, Encoding.UTF8);
			if (merge)
			{
				Dictionary<string, ChannelInfo> unique = new Dictionary<string, ChannelInfo>();
				foreach (ChannelInfo ch in allChannels)
				{
					string n = ChannelLogoHelper.CleanChannelName(ch.Name);
					if (!unique.ContainsKey(n))
					{
						unique[n] = ch;
					}
					else if (ch.Status == "可用" && unique[n].Status != "可用")
					{
						unique[n] = ch;
					}
				}
				foreach (KeyValuePair<string, ChannelInfo> item in unique)
				{
					ChannelInfo r = item.Value;
					string cleanName = ChannelLogoHelper.CleanChannelName(r.Name);
					string logo = ChannelLogoHelper.ResolveLogo(r.Name);
					sw.WriteLine(cleanName + "|" + r.Url + "|" + r.Location + "|" + r.Resolution + "|" + r.Speed + "|" + r.Group + "|" + r.Status + "|" + logo);
				}
				DarkMessageBox.Show($"成功导出 {unique.Count} 条数据（已合并相同频道）", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}
			foreach (ChannelInfo ch2 in allChannels)
			{
				string cleanName = ChannelLogoHelper.CleanChannelName(ch2.Name);
				string logo = ChannelLogoHelper.ResolveLogo(ch2.Name);
				sw.WriteLine(cleanName + "|" + ch2.Url + "|" + ch2.Location + "|" + ch2.Resolution + "|" + ch2.Speed + "|" + ch2.Group + "|" + ch2.Status + "|" + logo);
			}
			DarkMessageBox.Show($"成功导出 {allChannels.Count} 条数据", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("导出失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void CopyLink()
	{
		if (dgvData.SelectedRows.Count > 0)
		{
			string name = dgvData.SelectedRows[0].Cells[0].Value?.ToString() ?? "";
			string url = dgvData.SelectedRows[0].Cells[1].Value?.ToString() ?? "";
			if (!string.IsNullOrWhiteSpace(url))
			{
				string text = name + ", " + url;
				CopyTextToClipboard(text);
			}
		}
		else
		{
			DarkMessageBox.Show("请先选择一行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void CopyAllLinks()
	{
		if (allChannels.Count == 0)
		{
			DarkMessageBox.Show("列表为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		IEnumerable<string> lines = from c in allChannels
			where !string.IsNullOrWhiteSpace(c.Url)
			select c.Name + ", " + c.Url;
		string text = string.Join(Environment.NewLine, lines);
		CopyTextToClipboard(text);
	}

	private void SelectAllRows()
	{
		if (dgvData.Rows.Count == 0)
		{
			return;
		}
		dgvData.ClearSelection();
		foreach (DataGridViewRow item in (IEnumerable)dgvData.Rows)
		{
			item.Selected = true;
		}
		dgvData.Invalidate();
	}

	private void DeleteRow()
	{
		if (dgvData.SelectedRows.Count > 0)
		{
			if (DarkMessageBox.Show("确定删除选中行？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
			{
				return;
			}
			foreach (DataGridViewRow row in dgvData.SelectedRows.Cast<DataGridViewRow>().ToList())
			{
				string url = row.Cells[1].Value?.ToString();
				ChannelInfo ch = allChannels.FirstOrDefault((ChannelInfo c) => c.Url == url);
				if (ch != null)
				{
					allChannels.Remove(ch);
				}
			}
			RefreshGrid();
			UpdateGroupFilter();
			totalCount = allChannels.Count;
			RecalcStats();
			UpdateStatusBar();
			UpdateEmptyState();
			UpdateActionButtonsVisibility();
		}
		else
		{
			DarkMessageBox.Show("请先选择一行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void ViewDetails()
	{
		if (dgvData.SelectedRows.Count > 0)
		{
			DataGridViewRow r = dgvData.SelectedRows[0];
			DarkMessageBox.Show($"名称: {r.Cells[0].Value}\n链接: {r.Cells[1].Value}\n归属地: {r.Cells[2].Value}\n分辨率: {r.Cells[3].Value}\n响应速度: {r.Cells[4].Value}\n分组: {r.Cells[5].Value}\n状态: {r.Cells[6].Value}", "频道详情", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		else
		{
			DarkMessageBox.Show("请先选择一行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void ClearInvalidLinks()
	{
		_ = allChannels.Count;
		allChannels.RemoveAll((ChannelInfo c) => c.Status == "不可用");
		_ = allChannels.Count;
		RefreshGrid();
		UpdateGroupFilter();
		totalCount = allChannels.Count;
		RecalcStats();
		UpdateStatusBar();
		UpdateEmptyState();
		UpdateActionButtonsVisibility();
	}

	private void ClearAllLinks()
	{
		if (isDetecting)
		{
			DarkMessageBox.Show("检测正在进行中，请先停止检测后再清空列表。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		if (dgvData.Rows.Count == 0)
		{
			DarkMessageBox.Show("列表为空，无需清空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		allChannels.Clear();
		dgvData.Rows.Clear();
		totalCount = 0;
		detectedCount = 0;
		availableCount = 0;
		UpdateGroupFilter();
		UpdateStatusBar();
		UpdateEmptyState();
		UpdateActionButtonsVisibility();
	}

	// 合并键 -> 频道组（同名/同台归一），用于“合并+台标”导出
	private Dictionary<string, List<ChannelInfo>> BuildMergedGroups()
	{
		Dictionary<string, List<ChannelInfo>> merged = new Dictionary<string, List<ChannelInfo>>();
		foreach (ChannelInfo ch in allChannels)
		{
			string key = ChannelLogoHelper.NormalizeKey(ch.Name);
			if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(ch.Url))
			{
				if (!merged.ContainsKey(key))
				{
					merged[key] = new List<ChannelInfo>();
				}
				merged[key].Add(ch);
			}
		}
		return merged;
	}

	// m3u 合并导出：官方台名 + tvg-logo + 备份链接以 # 合并到同一行
	private void ExportToM3uMergeLogo(string filePath)
	{
		try
		{
			Dictionary<string, List<ChannelInfo>> merged = BuildMergedGroups();
			IOrderedEnumerable<List<ChannelInfo>> ordered = merged.Values
				.OrderBy((List<ChannelInfo> items) => ChannelLogoHelper.CategoryOrder(ChannelLogoHelper.ClassifyChannel(items[0].Name)))
				.ThenBy((List<ChannelInfo> items) => items[0].Name);
			using (StreamWriter sw = new StreamWriter(filePath, append: false, Encoding.UTF8))
			{
				sw.WriteLine("#EXTM3U");
				foreach (List<ChannelInfo> items in ordered)
				{
					string official = ChannelLogoHelper.OfficialName(items[0].Name);
					string logo = ChannelLogoHelper.ResolveLogo(items[0].Name);
					string group = ChannelLogoHelper.ClassifyChannel(items[0].Name);
					List<string> urls = items.Select((ChannelInfo c) => c.Url)
						.Where((string u) => !string.IsNullOrWhiteSpace(u))
						.Distinct()
						.ToList();
					if (urls.Count == 0)
					{
						continue;
					}
					string urlLine = string.Join("#", urls);
					sw.WriteLine("#EXTINF:-1 tvg-name=\"" + official + "\" tvg-logo=\"" + logo + "\" group-title=\"" + group + "\"," + official);
					sw.WriteLine(urlLine);
				}
			}
			DarkMessageBox.Show($"成功导出 {merged.Count} 个频道（含台标，合并 {allChannels.Count} 个源）", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("导出失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	// txt 合并导出：官方台名,url1#url2#...#urlN,logo
	private void ExportToTxtMergeLogo(string filePath)
	{
		try
		{
			Dictionary<string, List<ChannelInfo>> merged = BuildMergedGroups();
			// 按分类聚合，输出 #genre# 分节
			Dictionary<string, List<List<ChannelInfo>>> byCat = new Dictionary<string, List<List<ChannelInfo>>>();
			foreach (KeyValuePair<string, List<ChannelInfo>> kv in merged)
			{
				string cat = ChannelLogoHelper.ClassifyChannel(kv.Value[0].Name);
				if (!byCat.ContainsKey(cat))
				{
					byCat[cat] = new List<List<ChannelInfo>>();
				}
				byCat[cat].Add(kv.Value);
			}
			using (StreamWriter sw = new StreamWriter(filePath, append: false, Encoding.UTF8))
			{
				List<string> cats = byCat.Keys.OrderBy((string c) => ChannelLogoHelper.CategoryOrder(c)).ToList();
				foreach (string cat in cats)
				{
					sw.WriteLine(cat + ",#genre#");
					foreach (List<ChannelInfo> items in byCat[cat])
					{
						string official = ChannelLogoHelper.OfficialName(items[0].Name);
						string logo = ChannelLogoHelper.ResolveLogo(items[0].Name);
						List<string> urls = items.Select((ChannelInfo c) => c.Url)
							.Where((string u) => !string.IsNullOrWhiteSpace(u))
							.Distinct()
							.ToList();
						if (urls.Count == 0)
						{
							continue;
						}
						string urlLine = string.Join("#", urls);
						sw.WriteLine(official + "," + urlLine + "," + logo);
					}
				}
			}
			DarkMessageBox.Show($"成功导出 {merged.Count} 条数据（含台标，合并 {allChannels.Count} 个源）", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("导出失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}
}
