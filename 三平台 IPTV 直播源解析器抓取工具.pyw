import importlib
import subprocess
import sys
import os
import re
import json
import time
import threading
import queue
from tkinter import (
    Tk, Text, Button, Scrollbar, Frame, Entry, Menu, Label,
    Radiobutton, IntVar, PanedWindow, ttk, messagebox, StringVar, Listbox, END, Scale
)
from concurrent.futures import ThreadPoolExecutor, TimeoutError

# ===================== 全局常量配置【三平台路径统一修复，移除模板内http前缀】 =====================
class AppConfig:
    SUFFIX_TXT = ".txt"
    SUFFIX_M3U = ".m3u"
    FILE_ENCODING = "utf-8"
    # 平台1 智慧光迅：仅路径
    ZHGXTV_SUFFIX = "/ZHGXTV/Public/json/live_interface.txt"
    # 平台2 华视美达：仅路径，移除 http://{ip_port}
    HUASHI_TPL = "/newlive/live/hls/{cid}/live.m3u8"
    # 平台3 KUTV：仅路径，移除 http://{ip_port}
    KUTV_API_TPL = "/iptv/live/1000.json?key=txiptv"
    HUASHI_DEFAULT_RANGE = "1-100"
    REQ_TIMEOUT = 8
    HUASHI_CHECK_TIMEOUT = 2.5
    SCAN_WORKER_NUM = 8
    MAX_HISTORY = 10
    MAX_RETRY = 2  # 请求失败自动重试2次
    HEADERS_BASE = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36",
        "Accept-Language": "zh-CN,zh;q=0.9",
        "Accept": "*/*"
    }
    PREFIX_ZHGX = "智慧光迅"
    PREFIX_HUASHI = "华视美达"
    PREFIX_KUTV = "智能KUTV"

# ===================== 依赖自动安装 =====================
def install_requests():
    try:
        import requests
        return True
    except ImportError:
        print("未检测到requests，自动安装依赖...")
        try:
            cmd = [
                sys.executable, "-m", "pip", "install", "requests",
                "--quiet", "--disable-pip-version-check"
            ]
            subprocess.check_call(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            importlib.invalidate_caches()
            import requests
            print("requests 安装完成")
            return True
        except Exception as e:
            err_msg = f"自动安装失败，请手动执行：pip install requests\n错误详情：{str(e)}"
            print(err_msg)
            input("按回车退出程序")
            sys.exit(1)

install_requests()
import requests

# ===================== 通用工具函数 =====================
def clean_ip_port(raw_input: str):
    raw = raw_input.strip()
    if not raw:
        return False, "输入不能为空"
    # 匹配 http/https + IP:端口
    pattern = r'(?:http://|https://)?((?:\d{1,3}\.){3}\d{1,3}:\d+)'
    match = re.search(pattern, raw)
    if not match:
        return False, "IP端口格式错误，示例：110.72.103.69:8181"
    ip_port = match.group(1)
    if ":" not in ip_port:
        return False, "缺少端口号"
    ip_part, port_part = ip_port.split(":", 1)
    ip_segs = ip_part.split(".")
    if len(ip_segs) != 4:
        return False, f"IPv4分段错误：{ip_part}"
    for seg in ip_segs:
        if not seg.isdigit() or not 0 <= int(seg) <= 255:
            return False, f"非法IP段：{seg}"
    if not port_part.isdigit() or not 1 <= int(port_part) <= 65535:
        return False, f"非法端口：{port_part}"
    return True, ip_port

def safe_text_encode(text: str):
    if not text:
        return ""
    clean_str = re.sub(r'[\x00-\x1F\x7F]', "", text)
    return clean_str.encode("utf-8", errors="replace").decode("utf-8")

def get_work_dir():
    try:
        if hasattr(sys, '_MEIPASS'):
            return os.path.dirname(sys.executable)
        return os.path.dirname(os.path.abspath(__file__))
    except:
        return os.getcwd()

def get_unique_file_path(base_name: str, ext: str):
    work_dir = get_work_dir()
    num = 0
    while True:
        fname = f"{base_name}{ext}" if num == 0 else f"{base_name}_{num}{ext}"
        full_path = os.path.join(work_dir, fname)
        if not os.path.exists(full_path):
            return full_path
        num += 1

def safe_write_file(file_path: str, content: str):
    try:
        with open(file_path, "w", encoding=AppConfig.FILE_ENCODING) as f:
            f.write(content)
        return True, ""
    except PermissionError:
        return False, f"权限不足，无法写入 {os.path.basename(file_path)}"
    except OSError as e:
        return False, f"文件系统异常：{str(e)}"
    except Exception as e:
        return False, f"写入未知错误：{str(e)}"

def parse_header_text(header_text: str):
    headers = {}
    try:
        lines = header_text.strip().splitlines()
        for line in lines:
            if ":" not in line:
                continue
            k, v = line.split(":", 1)
            headers[k.strip()] = v.strip()
    except:
        pass
    return headers

def get_request_session(custom_headers=None, ip_port=""):
    sess = requests.Session()
    sess.headers.update(AppConfig.HEADERS_BASE)
    # 智慧光迅服务器必须携带IP Referer，解决403拦截
    if ip_port:
        ip_only = ip_port.split(":")[0]
        sess.headers["Referer"] = f"http://{ip_only}/"
    if custom_headers and isinstance(custom_headers, dict):
        sess.headers.update(custom_headers)
    return sess

# ===================== 平台2 华视美达 单流检测（修复路径拼接，外部统一加http根域名） =====================
def check_single_huashi(sess: requests.Session, ip_port: str, cid: int):
    root_http = f"http://{ip_port}"
    url = f"{root_http}{AppConfig.HUASHI_TPL}".format(cid=cid)
    try:
        resp = sess.head(url, timeout=AppConfig.HUASHI_CHECK_TIMEOUT, allow_redirects=True)
        if resp.status_code == 200:
            return cid, True, url
    except Exception:
        pass
    try:
        resp = sess.get(url, timeout=AppConfig.HUASHI_CHECK_TIMEOUT, stream=True)
        if resp.status_code == 200 and "m3u8" in resp.text[:500]:
            return cid, True, url
    except Exception:
        pass
    return cid, False, url

# ===================== 后台任务处理类（子线程分离，UI不阻塞） =====================
class BackendTask:
    def __init__(self, log_queue: queue.Queue, ui_callback):
        self.log_queue = log_queue
        self.ui_callback = ui_callback
        self.stop_flag = False
        self.result_data = None

    def log(self, msg):
        self.log_queue.put(f"{time.strftime('%H:%M:%S')} | {msg}")

    # 平台1：智慧光迅抓取逻辑
    def run_zhgx_task(self, ip_port, url, timeout, custom_hd):
        try:
            sess = get_request_session(custom_hd, ip_port)
            resp = None
            for retry in range(AppConfig.MAX_RETRY + 1):
                try:
                    resp = sess.get(url, timeout=timeout)
                    resp.raise_for_status()
                    break
                except Exception as e:
                    self.log(f"第{retry+1}次请求失败：{str(e)}，等待1秒重试...")
                    time.sleep(1)
            if resp is None:
                self.result_data = {"error": "多次请求服务器失败，无法连接接口"}
                return
            # 多编码兼容，解决空白返回
            raw_bytes = resp.content
            try:
                raw_text = raw_bytes.decode("utf-8")
            except:
                try:
                    raw_text = raw_bytes.decode("gbk")
                except:
                    raw_text = raw_bytes.decode("gb2312", errors="replace")
            raw_text = raw_text.strip()
            if not raw_text:
                self.result_data = {"error": "服务器返回空内容，无任何频道数据"}
                return
            valid_cnt, err_cnt = 0, 0
            preview_lines = []
            m3u_lines = ["#EXTM3U"]
            for line in raw_text.splitlines():
                line = line.strip()
                if not line:
                    continue
                try:
                    name, play_url = line.split(",", 1)
                    clean_name = safe_text_encode(name)
                    preview_lines.append(f"{clean_name} , {play_url}")
                    m3u_lines.append(f"#EXTINF:-1,{clean_name}")
                    m3u_lines.append(play_url)
                    valid_cnt += 1
                except:
                    err_cnt += 1
            m3u_content = "\n".join(m3u_lines)
            self.result_data = {
                "type": "zhgx",
                "ip_port": ip_port,
                "raw": raw_text,
                "preview": "\n".join(preview_lines),
                "m3u": m3u_content,
                "valid": valid_cnt,
                "err": err_cnt
            }
            self.log(f"智慧光迅解析完成，有效频道 {valid_cnt}，解析异常行 {err_cnt}")
        except Exception as e:
            self.result_data = {"error": f"任务运行异常：{str(e)}"}

    # 平台2：华视美达并发扫描
    def run_huashi_task(self, ip_port, timeout, range_str, thread_num, custom_hd):
        try:
            if "-" not in range_str:
                self.result_data = {"error": "扫描区间格式错误，标准示例：1-100"}
                return
            start_id, end_id = map(int, range_str.split("-"))
            if start_id <= 0 or end_id < start_id:
                self.result_data = {"error": "扫描区间数字非法，起始必须小于结束且大于0"}
                return
            sess = get_request_session(custom_hd, ip_port)
            cid_list = list(range(start_id, end_id + 1))
            m3u_lines = ["#EXTM3U"]
            valid_cnt = 0
            preview_lines = []
            self.log(f"开始并发扫描 ID {start_id}~{end_id}，并发线程 {thread_num}")
            with ThreadPoolExecutor(max_workers=thread_num) as pool:
                task_map = {pool.submit(check_single_huashi, sess, ip_port, c): c for c in cid_list}
                for task in task_map:
                    if self.stop_flag:
                        self.log("用户手动终止扫描任务")
                        pool.shutdown(wait=False, cancel_futures=True)
                        break
                    try:
                        cid, ok, url = task.result(timeout=AppConfig.HUASHI_CHECK_TIMEOUT + 1)
                    except TimeoutError:
                        self.log(f"ID {task_map[task]} 流检测超时")
                        continue
                    if ok:
                        valid_cnt += 1
                        log_msg = f"✅ ID:{cid} | {url}"
                        m3u_lines.append(f"#EXTINF:-1,华视频道{cid}")
                        m3u_lines.append(url)
                        preview_lines.append(log_msg)
                    else:
                        log_msg = f"❌ ID:{cid}"
                    self.log(log_msg)
            raw_summary = f"华视扫描汇总\n扫描区间：{start_id}-{end_id}\n总扫描数量：{len(cid_list)}\n有效频道：{valid_cnt}"
            m3u_content = "\n".join(m3u_lines)
            self.result_data = {
                "type": "huashi",
                "ip_port": ip_port,
                "raw": raw_summary,
                "preview": "\n".join(preview_lines),
                "m3u": m3u_content,
                "valid": valid_cnt,
                "scan_range": f"{start_id}-{end_id}"
            }
            self.log(f"华视扫描全部完成，有效频道 {valid_cnt}")
        except ValueError:
            self.result_data = {"error": "扫描区间/并发线程必须为纯数字"}
        except Exception as e:
            self.result_data = {"error": f"任务运行异常：{str(e)}"}

    # 平台3：智能KUTV JSON接口（修复URL拼接，无重复http）
    def run_kutv_task(self, ip_port, url, timeout, custom_hd):
        try:
            sess = get_request_session(custom_hd, ip_port)
            resp = None
            for retry in range(AppConfig.MAX_RETRY + 1):
                try:
                    resp = sess.get(url, timeout=timeout)
                    resp.raise_for_status()
                    break
                except Exception as e:
                    self.log(f"第{retry+1}次请求失败：{str(e)}，等待1秒重试...")
                    time.sleep(1)
            if resp is None:
                self.result_data = {"error": "多次请求服务器失败，无法连接JSON接口"}
                return
            resp.encoding = "utf-8"
            json_text = resp.text
            data = json.loads(json_text)
            if data.get("code") != 0:
                self.result_data = {"error": f"接口返回异常 code={data.get('code')} msg={data.get('msg')}"}
                return
            channel_arr = data.get("data", [])
            fmt_json = safe_text_encode(json.dumps(data, ensure_ascii=False, indent=2))
            preview_lines = []
            csv_lines = []
            m3u_lines = ["#EXTM3U"]
            valid_cnt = 0
            base_http = f"http://{ip_port}"
            for ch in channel_arr:
                raw_name = ch.get("name", "未知频道")
                ch_name = safe_text_encode(raw_name)
                rel_url = ch.get("url", "")
                if not rel_url:
                    continue
                # 补全完整播放地址
                full_play = f"{base_http}{rel_url}"
                csv_line = f"{ch_name},{full_play}"
                csv_lines.append(csv_line)
                preview_lines.append(csv_line)
                m3u_lines.append(f"#EXTINF:-1,{ch_name}")
                m3u_lines.append(full_play)
                valid_cnt += 1
            m3u_content = "\n".join(m3u_lines)
            self.result_data = {
                "type": "kutv",
                "ip_port": ip_port,
                "raw": fmt_json,
                "preview": "\n".join(preview_lines),
                "csv_list": "\n".join(csv_lines),
                "m3u": m3u_content,
                "valid": valid_cnt
            }
            self.log(f"智能KUTV JSON解析完成，有效频道 {valid_cnt}")
        except json.JSONDecodeError:
            self.result_data = {"error": "服务器返回内容不是标准JSON，接口失效或地址错误"}
        except Exception as e:
            self.result_data = {"error": f"任务运行异常：{str(e)}"}

# ===================== 主窗口UI类（Postman单窗口布局） =====================
class IPTVPostmanWindow:
    def __init__(self, root: Tk):
        self.root = root
        self.root.title("直播源抓取工具 Postman单窗口 三平台修复完整版")
        self.root.geometry("1300x820")
        self.root.minsize(1000, 600)
        self.root.protocol("WM_DELETE_WINDOW", self.on_window_close)
        # 线程安全日志队列
        self.log_queue = queue.Queue()
        self.task_running = False
        self.backend_task = None
        self.server_history = []
        self.preview_data = None
        # UI绑定变量
        self.var_platform = IntVar(value=1)
        self.var_url = StringVar()
        self.var_timeout = StringVar(value=str(AppConfig.REQ_TIMEOUT))
        self.var_huashi_range = StringVar(value=AppConfig.HUASHI_DEFAULT_RANGE)
        self.var_scan_thread = StringVar(value=str(AppConfig.SCAN_WORKER_NUM))
        self.var_status = StringVar(value="就绪 | 等待发送请求")
        self.var_stat_info = StringVar(value="频道：0 | 响应：无")
        # 初始化界面数据
        self.load_history()
        self.build_top_bar()
        self.build_main_pane()
        self.build_status_bar()
        self.bind_right_menu_all()
        self.switch_platform_view()
        self.poll_log_queue()

    # 窗口关闭，终止后台扫描
    def on_window_close(self):
        if self.backend_task:
            self.backend_task.stop_flag = True
        self.root.destroy()

    # 定时轮询日志队列，主线程安全刷新UI
    def poll_log_queue(self):
        try:
            while True:
                log_msg = self.log_queue.get_nowait()
                self.append_log_direct(log_msg)
        except queue.Empty:
            pass
        self.root.after(50, self.poll_log_queue)

    # 直接写入日志面板
    def append_log_direct(self, txt: str):
        try:
            self.text_log.config(state="normal")
            self.text_log.insert("end", f"{txt}\n")
            self.text_log.see("end")
            self.text_log.config(state="disabled")
        except:
            pass

    # 历史IP读写缓存
    def load_history(self):
        his_path = os.path.join(get_work_dir(), "history.cache")
        self.server_history = []
        if os.path.exists(his_path):
            try:
                with open(his_path, "r", encoding="utf-8") as f:
                    lines = f.readlines()
                self.server_history = [i.strip() for i in lines if i.strip()][:AppConfig.MAX_HISTORY]
            except Exception:
                self.server_history = []
        self.refresh_history_list()

    def save_history(self, addr):
        try:
            if addr not in self.server_history:
                self.server_history.insert(0, addr)
            self.server_history = self.server_history[:AppConfig.MAX_HISTORY]
            his_path = os.path.join(get_work_dir(), "history.cache")
            with open(his_path, "w", encoding="utf-8") as f:
                f.write("\n".join(self.server_history))
            self.refresh_history_list()
        except Exception:
            pass

    def refresh_history_list(self):
        try:
            self.history_listbox.delete(0, END)
            for item in self.server_history:
                self.history_listbox.insert(END, item)
        except:
            pass

    # 顶部地址栏
    def build_top_bar(self):
        top_frame = Frame(self.root)
        top_frame.pack(fill="x", padx=8, pady=6)
        Label(top_frame, text="GET", font=("微软雅黑", 10, "bold"), bg="#7b61ff", fg="white").pack(side="left", padx=(0, 6))
        Entry(top_frame, textvariable=self.var_url, font=("微软雅黑", 10)).pack(side="left", fill="x", expand=True, padx=6)
        self.btn_send = Button(top_frame, text="发送", width=8, bg="#7b61ff", fg="white", command=self.send_request)
        self.btn_send.pack(side="left", padx=4)
        Button(top_frame, text="清空", width=6, command=self.clear_all).pack(side="left", padx=4)
        Button(top_frame, text="导出全部文件", width=12, bg="#28a745", fg="white", command=self.export_all_file).pack(side="left", padx=4)
        Button(top_frame, text="停止扫描", width=10, bg="#dc3545", fg="white", command=self.stop_scan).pack(side="left", padx=4)

    # 左右可拖拽分割主面板
    def build_main_pane(self):
        main_pane = PanedWindow(self.root, orient="horizontal", sashwidth=6)
        main_pane.pack(fill="both", expand=True, padx=8, pady=(0, 8))
        # 左侧配置面板
        left_frame = Frame(main_pane, width=360)
        main_pane.add(left_frame, minsize=320)
        self.build_left_config(left_frame)
        # 右侧返回结果面板
        right_frame = Frame(main_pane)
        main_pane.add(right_frame)
        self.build_right_response(right_frame)

    # 左侧多标签配置页
    def build_left_config(self, parent):
        notebook = ttk.Notebook(parent)
        notebook.pack(fill="both", expand=True)
        # Tab1 服务器地址输入
        tab_addr = Frame(notebook)
        notebook.add(tab_addr, text="服务器地址")
        Label(tab_addr, text="IP端口（示例：110.72.103.69:8181）", font=("微软雅黑", 9)).pack(anchor="w", padx=6, pady=4)
        self.entry_ip = Entry(tab_addr, font=("微软雅黑", 10))
        self.entry_ip.pack(fill="x", padx=6, pady=2)
        Button(tab_addr, text="自动拼接完整URL", command=self.auto_build_url).pack(anchor="w", padx=6, pady=4)
        Label(tab_addr, text="历史服务器（双击填入）", font=("微软雅黑", 9)).pack(anchor="w", padx=6, pady=(10, 4))
        his_frame = Frame(tab_addr)
        his_frame.pack(fill="both", expand=True, padx=6, pady=2)
        self.history_listbox = Listbox(his_frame)
        scroll_his = Scrollbar(his_frame, command=self.history_listbox.yview)
        self.history_listbox.configure(yscrollcommand=scroll_his.set)
        self.history_listbox.pack(side="left", fill="both", expand=True)
        scroll_his.pack(side="right", fill="y")
        self.history_listbox.bind("<Double-Button-1>", self.fill_history_ip)

        # Tab2 平台参数切换
        tab_platform = Frame(notebook)
        notebook.add(tab_platform, text="平台参数")
        Radiobutton(tab_platform, text="1.智慧光迅 ZHGXTV", variable=self.var_platform, value=1, command=self.switch_platform_view).pack(anchor="w", padx=6, pady=3)
        Radiobutton(tab_platform, text="2.华视美达 频道扫描", variable=self.var_platform, value=2, command=self.switch_platform_view).pack(anchor="w", padx=6, pady=3)
        Radiobutton(tab_platform, text="3.智能KUTV JSON接口", variable=self.var_platform, value=3, command=self.switch_platform_view).pack(anchor="w", padx=6, pady=3)
        self.huashi_param_frame = Frame(tab_platform)
        Label(self.huashi_param_frame, text="扫描ID区间(起始-结束)").pack(anchor="w", padx=6, pady=(10, 2))
        Entry(self.huashi_param_frame, textvariable=self.var_huashi_range).pack(fill="x", padx=6, pady=2)
        Label(self.huashi_param_frame, text="并发扫描线程数").pack(anchor="w", padx=6, pady=4)
        Scale(self.huashi_param_frame, from_=1, to=20, variable=self.var_scan_thread, orient="horizontal").pack(fill="x", padx=6)

        # Tab3 请求配置（超时、自定义请求头）
        tab_header = Frame(notebook)
        notebook.add(tab_header, text="请求配置")
        Label(tab_header, text="请求超时(秒)").pack(anchor="w", padx=6, pady=4)
        Entry(tab_header, textvariable=self.var_timeout).pack(fill="x", padx=6, pady=2)
        Label(tab_header, text="自定义请求头（一行一个 key:value）").pack(anchor="w", padx=6, pady=(10, 4))
        header_frame = Frame(tab_header)
        header_frame.pack(fill="both", expand=True, padx=6, pady=2)
        self.text_header = Text(header_frame, font=("微软雅黑", 9), height=8)
        scroll_hd = Scrollbar(header_frame, command=self.text_header.yview)
        self.text_header.configure(yscrollcommand=scroll_hd.set)
        self.text_header.pack(side="left", fill="both", expand=True)
        scroll_hd.pack(side="right", fill="y")
        default_hd_text = "\n".join([f"{k}: {v}" for k, v in AppConfig.HEADERS_BASE.items()])
        self.text_header.insert("end", default_hd_text)

    # 切换平台，显示/隐藏华视专属参数
    def switch_platform_view(self):
        plat = self.var_platform.get()
        if plat == 2:
            self.huashi_param_frame.pack(fill="x", pady=6)
        else:
            self.huashi_param_frame.pack_forget()

    # 右侧结果多标签：Raw/预览/M3U/日志
    def build_right_response(self, parent):
        notebook = ttk.Notebook(parent)
        notebook.pack(fill="both", expand=True)
        # Raw原始文本
        tab_raw = Frame(notebook)
        notebook.add(tab_raw, text="Raw 原始文本")
        raw_frame = Frame(tab_raw)
        raw_frame.pack(fill="both", expand=True, padx=4, pady=4)
        self.text_raw = Text(raw_frame, font=("微软雅黑", 9))
        scroll_raw = Scrollbar(raw_frame, command=self.text_raw.yview)
        self.text_raw.configure(yscrollcommand=scroll_raw.set)
        self.text_raw.pack(side="left", fill="both", expand=True)
        scroll_raw.pack(side="right", fill="y")
        # 频道预览清单
        tab_preview = Frame(notebook)
        notebook.add(tab_preview, text="频道预览清单")
        prev_frame = Frame(tab_preview)
        prev_frame.pack(fill="both", expand=True, padx=4, pady=4)
        self.text_preview = Text(prev_frame, font=("微软雅黑", 9))
        scroll_prev = Scrollbar(prev_frame, command=self.text_preview.yview)
        self.text_preview.configure(yscrollcommand=scroll_prev.set)
        self.text_preview.pack(side="left", fill="both", expand=True)
        scroll_prev.pack(side="right", fill="y")
        # M3U播放列表
        tab_m3u = Frame(notebook)
        notebook.add(tab_m3u, text="M3U 播放列表")
        m3u_frame = Frame(tab_m3u)
        m3u_frame.pack(fill="both", expand=True, padx=4, pady=4)
        self.text_m3u = Text(m3u_frame, font=("微软雅黑", 9))
        scroll_m3u = Scrollbar(m3u_frame, command=self.text_m3u.yview)
        self.text_m3u.configure(yscrollcommand=scroll_m3u.set)
        self.text_m3u.pack(side="left", fill="both", expand=True)
        scroll_m3u.pack(side="right", fill="y")
        # 运行日志
        tab_log = Frame(notebook)
        notebook.add(tab_log, text="运行日志")
        log_frame = Frame(tab_log)
        log_frame.pack(fill="both", expand=True, padx=4, pady=4)
        self.text_log = Text(log_frame, font=("微软雅黑", 9))
        scroll_log = Scrollbar(log_frame, command=self.text_log.yview)
        self.text_log.configure(yscrollcommand=scroll_log.set)
        self.text_log.pack(side="left", fill="both", expand=True)
        scroll_log.pack(side="right", fill="y")

    # 底部状态栏
    def build_status_bar(self):
        status_frame = Frame(self.root, bg="#f0f0f0")
        status_frame.pack(fill="x", padx=8, pady=4)
        Label(status_frame, textvariable=self.var_status, bg="#f0f0f0", font=("微软雅黑", 9)).pack(side="left")
        Label(status_frame, textvariable=self.var_stat_info, bg="#f0f0f0", font=("微软雅黑", 9)).pack(side="right")

    # 全局文本框右键复制菜单
    def bind_right_menu_all(self):
        def bind_text_copy(widget):
            menu = Menu(widget, tearoff=0)
            def copy_all():
                content = widget.get("1.0", "end-1c")
                self.root.clipboard_clear()
                self.root.clipboard_append(content)
            menu.add_command(label="复制全部内容", command=copy_all)
            widget.bind("<Button-3>", lambda e: menu.post(e.x_root, e.y_root))
        bind_text_copy(self.text_raw)
        bind_text_copy(self.text_preview)
        bind_text_copy(self.text_m3u)
        bind_text_copy(self.text_log)
        # 输入框通用右键菜单
        entry_menu = Menu(self.root, tearoff=0)
        entry_menu.add_command(label="剪切", command=lambda: self.root.focus_get().event_generate("<<Cut>>"))
        entry_menu.add_command(label="复制", command=lambda: self.root.focus_get().event_generate("<<Copy>>"))
        entry_menu.add_command(label="粘贴", command=lambda: self.root.focus_get().event_generate("<<Paste>>"))
        entry_menu.add_separator()
        entry_menu.add_command(label="全选", command=lambda: self.root.focus_get().select_range(0, "end"))
        def entry_right_click(event):
            focus_wid = self.root.focus_get()
            if isinstance(focus_wid, Entry):
                entry_menu.post(event.x_root, event.y_root)
        self.root.bind("<Button-3>", entry_right_click)

    # 清空所有面板数据
    def clear_all(self):
        try:
            for text_widget in [self.text_raw, self.text_preview, self.text_m3u, self.text_log]:
                text_widget.config(state="normal")
                text_widget.delete("1.0", "end")
                text_widget.config(state="disabled")
            self.var_status.set("就绪 | 已清空所有内容")
            self.var_stat_info.set("频道：0 | 响应：无")
            self.preview_data = None
        except:
            pass

    # 双击历史记录填充IP输入框
    def fill_history_ip(self, event):
        try:
            sel = self.history_listbox.curselection()
            if not sel:
                return
            val = self.history_listbox.get(sel[0])
            self.entry_ip.delete(0, END)
            self.entry_ip.insert(0, val)
            self.auto_build_url()
        except:
            pass

    # ===================== 核心修复函数：自动拼接URL（解决双http重复BUG） =====================
    def auto_build_url(self):
        ip_raw = self.entry_ip.get().strip()
        # 兼容用户直接粘贴完整http链接，跳过拼接
        if ip_raw.startswith("http://") or ip_raw.startswith("https://"):
            self.var_url.set(ip_raw)
            self.var_status.set("检测到完整HTTP链接，直接使用，无需拼接")
            return
        # 校验IP端口格式
        ok, ip_port = clean_ip_port(ip_raw)
        if not ok:
            self.var_status.set(f"地址校验失败：{ip_port}")
            self.append_log_direct(f"IP校验错误：{ip_port}")
            return
        plat = self.var_platform.get()
        root_http = f"http://{ip_port}"
        if plat == 1:
            # 智慧光迅
            full_url = f"{root_http}{AppConfig.ZHGXTV_SUFFIX}"
        elif plat == 2:
            # 华视仅展示根地址，扫描时动态拼接CID路径
            full_url = root_http
        else:
            # KUTV 根地址 + 纯接口路径，无重复http
            full_url = f"{root_http}{AppConfig.KUTV_API_TPL}"
        self.var_url.set(full_url)
        self.var_status.set(f"已自动拼接标准地址：{full_url}")

    # 停止后台扫描任务
    def stop_scan(self):
        if self.backend_task:
            self.backend_task.stop_flag = True
            self.append_log_direct("用户触发停止扫描指令")
            self.var_status.set("正在终止后台扫描任务...")

    # 后台任务完成后刷新UI数据面板
    def task_finish_callback(self):
        self.btn_send.config(state="normal")
        self.task_running = False
        data = self.backend_task.result_data
        if data is None:
            self.var_status.set("任务未返回任何数据")
            return
        # 捕获任务错误
        if "error" in data:
            self.var_status.set(f"任务失败：{data['error']}")
            self.append_log_direct(f"【运行错误】{data['error']}")
            return
        # 填充Raw原始文本
        self.text_raw.config(state="normal")
        self.text_raw.delete("1.0", "end")
        self.text_raw.insert("end", data["raw"])
        self.text_raw.config(state="disabled")
        # 填充频道预览
        self.text_preview.config(state="normal")
        self.text_preview.delete("1.0", "end")
        self.text_preview.insert("end", data["preview"])
        self.text_preview.config(state="disabled")
        # 填充M3U播放列表
        self.text_m3u.config(state="normal")
        self.text_m3u.delete("1.0", "end")
        self.text_m3u.insert("end", data["m3u"])
        self.text_m3u.config(state="disabled")
        # 缓存导出数据
        self.preview_data = data
        self.var_stat_info.set(f"有效频道：{data['valid']}")
        self.var_status.set("抓取完成，可预览或导出文件")

    # 发送按钮入口，创建守护子线程，不阻塞GUI
    def send_request(self):
        if self.task_running:
            messagebox.showinfo("操作提示", "当前已有任务正在运行，请等待完成或点击停止扫描")
            return
        self.clear_all()
        full_url = self.var_url.get().strip()
        ip_raw = self.entry_ip.get().strip()
        # 输入校验
        ok, ip_port = clean_ip_port(ip_raw)
        if not ok:
            self.var_status.set(f"IP地址非法：{ip_port}")
            self.append_log_direct(f"输入校验失败：{ip_port}")
            return
        self.save_history(ip_port)
        # 读取请求参数
        try:
            timeout = int(self.var_timeout.get())
        except:
            timeout = AppConfig.REQ_TIMEOUT
        header_text = self.text_header.get("1.0", "end-1c")
        custom_hd = parse_header_text(header_text)
        plat = self.var_platform.get()
        self.append_log_direct(f"===== 启动任务 | 平台{plat} | 服务器 {ip_port} =====")
        self.var_status.set("后台执行中，界面可正常操作...")
        self.btn_send.config(state="disabled")
        self.task_running = True
        # 初始化后台任务
        self.backend_task = BackendTask(self.log_queue, self.task_finish_callback)
        # 根据平台启动对应子线程
        if plat == 1:
            t = threading.Thread(target=self.backend_task.run_zhgx_task, args=(ip_port, full_url, timeout, custom_hd), daemon=True)
        elif plat == 2:
            range_str = self.var_huashi_range.get().strip()
            thread_num = int(self.var_scan_thread.get())
            t = threading.Thread(target=self.backend_task.run_huashi_task, args=(ip_port, timeout, range_str, thread_num, custom_hd), daemon=True)
        else:
            t = threading.Thread(target=self.backend_task.run_kutv_task, args=(ip_port, full_url, timeout, custom_hd), daemon=True)
        t.start()
        # 监听线程结束，回调刷新UI
        def wait_thread_finish():
            t.join()
            self.root.after(0, self.task_finish_callback)
        threading.Thread(target=wait_thread_finish, daemon=True).start()

    # 一键导出所有生成文件
    def export_all_file(self):
        if not self.preview_data:
            messagebox.showwarning("操作提示", "请先点击【发送】抓取接口数据后再执行导出！")
            return
        data = self.preview_data
        ip_safe = data["ip_port"].replace(":", "_")
        err_list = []
        msg = ""
        # 平台1：智慧光迅 导出原始txt + m3u
        if data["type"] == "zhgx":
            txt_path = get_unique_file_path(f"{AppConfig.PREFIX_ZHGX}_原始_{ip_safe}", AppConfig.SUFFIX_TXT)
            m3u_path = get_unique_file_path(f"{AppConfig.PREFIX_ZHGX}_直播列表_{ip_safe}", AppConfig.SUFFIX_M3U)
            ok1, e1 = safe_write_file(txt_path, data["raw"])
            ok2, e2 = safe_write_file(m3u_path, data["m3u"])
            if not ok1: err_list.append(e1)
            if not ok2: err_list.append(e2)
            msg = f"导出成功\n原始文本：{os.path.basename(txt_path)}\nM3U播放列表：{os.path.basename(m3u_path)}\n有效频道：{data['valid']}"
        # 平台2：华视美达 仅导出m3u
        elif data["type"] == "huashi":
            m3u_path = get_unique_file_path(f"{AppConfig.PREFIX_HUASHI}_有效源_{ip_safe}", AppConfig.SUFFIX_M3U)
            ok, e = safe_write_file(m3u_path, data["m3u"])
            if not ok: err_list.append(e)
            msg = f"导出成功\nM3U文件：{os.path.basename(m3u_path)}\n有效频道：{data['valid']}"
        # 平台3：KUTV 导出逗号清单txt、原始JSON、m3u三类文件
        elif data["type"] == "kutv":
            txt_csv = get_unique_file_path(f"{AppConfig.PREFIX_KUTV}_逗号清单_{ip_safe}", AppConfig.SUFFIX_TXT)
            txt_json = get_unique_file_path(f"{AppConfig.PREFIX_KUTV}_原始JSON_{ip_safe}", AppConfig.SUFFIX_TXT)
            m3u_path = get_unique_file_path(f"{AppConfig.PREFIX_KUTV}_直播列表_{ip_safe}", AppConfig.SUFFIX_M3U)
            ok1, e1 = safe_write_file(txt_csv, data["csv_list"])
            ok2, e2 = safe_write_file(txt_json, data["raw"])
            ok3, e3 = safe_write_file(m3u_path, data["m3u"])
            if not ok1: err_list.append(e1)
            if not ok2: err_list.append(e2)
            if not ok3: err_list.append(e3)
            msg = f"导出3个文件成功\n逗号清单txt / 原始JSON / M3U播放列表\n有效频道：{data['valid']}"
        # 导出结果弹窗提示
        if err_list:
            messagebox.showerror("导出部分失败", "\n".join(err_list))
            self.append_log_direct(f"导出异常：{';'.join(err_list)}")
        else:
            messagebox.showinfo("导出完成", msg)
            self.append_log_direct("全部文件导出成功")

# ===================== 程序主入口 =====================
def main():
    root = Tk()
    app = IPTVPostmanWindow(root)
    root.mainloop()

if __name__ == "__main__":
    main()