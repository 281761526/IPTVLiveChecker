package com.family.iptvchecker.ui.player

import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.KeyEvent
import android.view.LayoutInflater
import android.view.SurfaceHolder
import android.view.View
import android.view.ViewGroup
import androidx.activity.OnBackPressedCallback
import androidx.fragment.app.Fragment
import androidx.fragment.app.activityViewModels
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import kotlinx.coroutines.launch
import android.widget.Toast
import com.family.iptvchecker.databinding.FragmentPlayerBinding
import com.family.iptvchecker.player.ChannelPlayer
import com.family.iptvchecker.ui.list.ListViewModel

/**
 * 播放页（F7/F8/F13/F16）。
 *
 * 硬纪律：
 * - Surface **先解绑再销毁**（surfaceDestroyed → detachSurface，R17）；
 * - 起播失败/超时 Toast 提示并**自动退回列表**，焦点还原到该频道行；
 * - 硬解回落软解必须提示「已切换为软解，画面可能卡顿」（不许静默）；
 * - 不显示播放进度条（直播流 duration 可能是 inf，设计决策）；
 * - 返回键分级：先收起信息浮层 → 再按才回列表。
 *
 * 换台（F13）：↑↓/频道增减键在**可用频道**间切换，不退回列表。
 */
class PlayerFragment : Fragment() {

    /** 播放页关闭回调（宿主 Activity 实现，用于焦点还原）。 */
    interface Host {
        fun onPlayerClosed(channelId: String)
    }

    companion object {
        private const val ARG_CHANNEL_ID = "channel_id"
        private const val START_TIMEOUT_MS = 20_000L

        fun newInstance(channelId: String) = PlayerFragment().apply {
            arguments = Bundle().apply { putString(ARG_CHANNEL_ID, channelId) }
        }
    }

    private var b: FragmentPlayerBinding? = null
    private val vm: ListViewModel by activityViewModels()

    private var channelId: String = ""
    private val handler = Handler(Looper.getMainLooper())
    private var loaded = false
    private var paused = false
    private var surfaceReady = false
    private var overlayShown = false
    private var searchPanelShown = false

    private lateinit var leftAdapter: PanelChannelAdapter
    private lateinit var rightAdapter: PanelChannelAdapter

    private val startTimeout = Runnable {
        if (!loaded) fail("起播超时：源无响应")
    }

    private val hideInfoRunnable = Runnable {
        b?.channelInfo?.animate()?.alpha(0f)?.setDuration(300)?.start()
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        channelId = requireArguments().getString(ARG_CHANNEL_ID).orEmpty()
    }

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?
    ): View {
        val binding = FragmentPlayerBinding.inflate(inflater, container, false)
        b = binding
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        val binding = b ?: return
        binding.playerRoot.requestFocus()

        // —— Surface 生命周期（先绑定，销毁前解绑）——
        binding.playerSurface.holder.addCallback(object : SurfaceHolder.Callback {
            override fun surfaceCreated(holder: SurfaceHolder) {
                surfaceReady = true
                if (!ChannelPlayer.ensureCreated(requireContext())) {
                    fail("播放器初始化失败")
                    return
                }
                ChannelPlayer.mpv.attachSurface(holder)
                startPlay(channelId)
            }

            override fun surfaceChanged(holder: SurfaceHolder, format: Int, w: Int, h: Int) = Unit

            override fun surfaceDestroyed(holder: SurfaceHolder) {
                surfaceReady = false
                ChannelPlayer.mpv.detachSurface()   // 必须先解绑再销毁（R17）
            }
        })

        // —— 播放器事件（本 Fragment 存活期间接管）——
        ChannelPlayer.mpv.onFileLoaded = {
            runOnUi {
                loaded = true
                binding.loadingText.visibility = View.GONE
            }
        }
        ChannelPlayer.mpv.onPlayError = {
            runOnUi { if (!loaded) fail("起播失败：源不可用或格式不支持") }
        }
        ChannelPlayer.mpv.onSoftFallback = { msg ->
            runOnUi { Toast.makeText(requireContext(), msg, Toast.LENGTH_LONG).show() }
        }

        // —— 返回键分级（F12/§5.4）：先收面板 → 再收浮层 → 再按回列表 ——
        val back = object : OnBackPressedCallback(true) {
            override fun handleOnBackPressed() {
                if (searchPanelShown) {
                    hideSearchPanel()
                } else if (overlayShown) {
                    hideOverlay()
                } else {
                    quit()
                }
            }
        }
        requireActivity().onBackPressedDispatcher.addCallback(viewLifecycleOwner, back)

        // —— 按键：↑↓ 换台 / OK 暂停 / 数字跳台 / INFO 浮层 ——
        binding.playerRoot.setOnKeyListener { _, keyCode, event ->
            if (event.action != KeyEvent.ACTION_DOWN) return@setOnKeyListener false
            handleKey(keyCode, event)
        }

        renderInfo()

        // 数字回显（与列表页共用同一数据源）
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(androidx.lifecycle.Lifecycle.State.STARTED) {
                vm.digitDisplay.collect { text ->
                    val dv = b?.playerDigitDisplay ?: return@collect
                    if (text.isNullOrEmpty()) {
                        dv.visibility = View.GONE
                    } else {
                        dv.text = text
                        dv.visibility = View.VISIBLE
                    }
                }
            }
        }

        // —— APTV 频道搜索与选择面板初始化 ——
        setupSearchPanel()
    }

    private fun setupSearchPanel() {
        val binding = b ?: return
        leftAdapter = PanelChannelAdapter { ch ->
            switchTo(ch.id)
            hideSearchPanel()
        }
        rightAdapter = PanelChannelAdapter { ch ->
            switchTo(ch.id)
            hideSearchPanel()
        }

        binding.panelChannelSearch.recyclerLeftChannels.layoutManager = androidx.recyclerview.widget.LinearLayoutManager(requireContext())
        binding.panelChannelSearch.recyclerLeftChannels.adapter = leftAdapter

        binding.panelChannelSearch.recyclerRightResults.layoutManager = androidx.recyclerview.widget.LinearLayoutManager(requireContext())
        binding.panelChannelSearch.recyclerRightResults.adapter = rightAdapter

        // 填充左侧频道列表
        val channels = vm.repo.channels.value
        leftAdapter.submitList(channels)
        rightAdapter.submitList(channels)

        // 右侧实时搜索过滤
        binding.panelChannelSearch.etSearchBox.addTextChangedListener(object : android.text.TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) = Unit
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) = Unit
            override fun afterTextChanged(s: android.text.Editable?) {
                val query = s?.toString()?.trim().orEmpty()
                val filtered = if (query.isEmpty()) {
                    channels
                } else {
                    channels.filter { it.name.contains(query, ignoreCase = true) || it.group.contains(query, ignoreCase = true) }
                }
                rightAdapter.submitList(filtered)
            }
        })
    }

    private fun toggleSearchPanel() {
        if (searchPanelShown) hideSearchPanel() else showSearchPanel()
    }

    private fun showSearchPanel() {
        searchPanelShown = true
        b?.channelSearchPanelContainer?.visibility = View.VISIBLE
        b?.panelChannelSearch?.etSearchBox?.requestFocus()
    }

    private fun hideSearchPanel() {
        searchPanelShown = false
        b?.channelSearchPanelContainer?.visibility = View.GONE
        b?.playerRoot?.requestFocus()
    }

    override fun onStart() {
        super.onStart()
        // 全屏沉浸（隐藏系统栏）
        @Suppress("DEPRECATION")
        view?.systemUiVisibility =
            View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY or
                View.SYSTEM_UI_FLAG_FULLSCREEN or
                View.SYSTEM_UI_FLAG_HIDE_NAVIGATION or
                View.SYSTEM_UI_FLAG_LAYOUT_STABLE or
                View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN or
                View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
    }

    // —— 按键处理 ——

    private fun handleKey(keyCode: Int, event: KeyEvent): Boolean {
        // 任意键复现频道信息（3 秒后再次淡出）
        showInfoTemporarily()

        if (event.repeatCount > 0 &&
            keyCode != KeyEvent.KEYCODE_DPAD_UP && keyCode != KeyEvent.KEYCODE_DPAD_DOWN
        ) {
            return true   // 数字/OK 等不响应长按重复
        }

        val digit = com.family.iptvchecker.util.KeyCodes.digitOf(keyCode)
        if (digit >= 0) {
            vm.onDigit(digit)
            return true
        }
        return when {
            keyCode == KeyEvent.KEYCODE_DPAD_UP -> { if (!searchPanelShown) { switchRelative(-1); true } else false }
            keyCode == KeyEvent.KEYCODE_DPAD_DOWN -> { if (!searchPanelShown) { switchRelative(1); true } else false }
            com.family.iptvchecker.util.KeyCodes.isChannelUp(keyCode) -> { switchRelative(1); true }
            com.family.iptvchecker.util.KeyCodes.isChannelDown(keyCode) -> { switchRelative(-1); true }
            keyCode == KeyEvent.KEYCODE_DPAD_CENTER ||
                keyCode == KeyEvent.KEYCODE_ENTER ||
                keyCode == KeyEvent.KEYCODE_MEDIA_PLAY_PAUSE -> { if (!searchPanelShown) { togglePause(); true } else false }
            keyCode == KeyEvent.KEYCODE_INFO -> { toggleOverlay(); true }
            keyCode == KeyEvent.KEYCODE_SEARCH || keyCode == KeyEvent.KEYCODE_MENU -> { toggleSearchPanel(); true }
            else -> false
        }
    }

    private fun showInfoTemporarily() {
        val info = b?.channelInfo ?: return
        info.animate().cancel()
        info.alpha = 1f
        handler.removeCallbacks(hideInfoRunnable)
        handler.postDelayed(hideInfoRunnable, 3000)
    }

    private fun togglePause() {
        if (!loaded) return
        paused = !paused
        if (paused) ChannelPlayer.mpv.pause() else ChannelPlayer.mpv.resume()
        Toast.makeText(
            requireContext(),
            if (paused) "已暂停" else "继续播放",
            Toast.LENGTH_SHORT
        ).show()
    }

    private fun toggleOverlay() {
        if (overlayShown) hideOverlay() else showOverlay()
    }

    private fun showOverlay() {
        overlayShown = true
        val ch = vm.channelById(channelId) ?: return
        b?.apply {
            overlayLine1.text = "${ch.name} · ${vm.resolutionLabelOf(ch)} · ${ch.group}"
            overlayLine2.text = "${ch.url}\n状态 ${vm.statusLabelOf(ch)} · 耗时 ${ch.latencyMs}ms"
            infoOverlay.visibility = View.VISIBLE
        }
    }

    private fun hideOverlay() {
        overlayShown = false
        b?.infoOverlay?.visibility = View.GONE
    }

    // —— 换台（F13：可用频道间切换，跳过不可用，不退回列表）——

    private fun switchRelative(delta: Int) {
        val available = vm.availableChannels()
        if (available.isEmpty()) {
            Toast.makeText(requireContext(), "没有可用频道", Toast.LENGTH_SHORT).show()
            return
        }
        val curIdx = available.indexOfFirst { it.id == channelId }
        val base = if (curIdx < 0) 0 else curIdx
        val next = (base + delta + available.size) % available.size   // 首末循环
        switchTo(available[next].id, announce = "第 ${next + 1} 个 · ${available[next].name}")
    }

    /** 外部（数字跳台）调用的换台入口。 */
    fun switchTo(id: String, announce: String? = null) {
        if (id == channelId && loaded) return
        channelId = id
        loaded = false
        paused = false
        overlayShown = false
        vm.markPlayed(id)
        renderInfo()
        hideOverlay()
        b?.loadingText?.visibility = View.VISIBLE
        startPlay(id)
    }

    private fun startPlay(id: String) {
        val ch = vm.channelById(id) ?: run { fail("频道不存在"); return }
        val mpv = ChannelPlayer.mpv
        if (!mpv.isCreated || !surfaceReady) return
        handler.removeCallbacks(startTimeout)
        handler.postDelayed(startTimeout, START_TIMEOUT_MS)
        showInfoTemporarily()
        mpv.loadFile(ch.url)
    }

    private fun renderInfo() {
        val ch = vm.channelById(channelId) ?: return
        b?.apply {
            infoName.text = ch.name
            infoSub.text = "${ch.group} · ${vm.resolutionLabelOf(ch)}"
            channelInfo.alpha = 1f
        }
        showInfoTemporarily()
    }

    // —— 失败与退出（不允许静默）——

    private fun fail(msg: String) {
        runOnUi {
            Toast.makeText(requireContext(), msg, Toast.LENGTH_LONG).show()
            quit()
        }
    }

    private fun quit() {
        handler.removeCallbacks(startTimeout)
        handler.removeCallbacks(hideInfoRunnable)
        ChannelPlayer.mpv.onFileLoaded = null
        ChannelPlayer.mpv.onPlayError = null
        ChannelPlayer.mpv.onSoftFallback = null
        if (ChannelPlayer.mpv.isCreated) ChannelPlayer.mpv.stop()
        vm.clearDigitBuffer()
        val id = channelId
        (activity as? Host)?.onPlayerClosed(id)
        parentFragmentManager.beginTransaction().remove(this).commitAllowingStateLoss()
    }

    private fun runOnUi(block: () -> Unit) {
        if (!isAdded) return
        view?.post(block) ?: handler.post(block)
    }

    override fun onDestroyView() {
        handler.removeCallbacksAndMessages(null)
        // 解绑由 surfaceDestroyed 保证；这里仅清引用
        b = null
        super.onDestroyView()
    }
}

class PanelChannelAdapter(
    private val onClick: (com.family.iptvchecker.data.model.Channel) -> Unit
) : androidx.recyclerview.widget.RecyclerView.Adapter<PanelChannelAdapter.VH>() {
    private var list = listOf<com.family.iptvchecker.data.model.Channel>()

    fun submitList(newList: List<com.family.iptvchecker.data.model.Channel>) {
        list = newList
        notifyDataSetChanged()
    }

    class VH(val view: View) : androidx.recyclerview.widget.RecyclerView.ViewHolder(view) {
        val nameView: android.widget.TextView = view.findViewById(com.family.iptvchecker.R.id.tvPanelChannelName)
        val resView: android.widget.TextView = view.findViewById(com.family.iptvchecker.R.id.tvPanelResolution)
        val latView: android.widget.TextView = view.findViewById(com.family.iptvchecker.R.id.tvPanelLatency)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): VH {
        val v = LayoutInflater.from(parent.context).inflate(com.family.iptvchecker.R.layout.item_panel_channel, parent, false)
        return VH(v)
    }

    override fun onBindViewHolder(holder: VH, position: Int) {
        val ch = list[position]
        holder.nameView.text = ch.name
        holder.resView.text = ch.resolution.ifEmpty { "1080p" }
        holder.latView.text = if (ch.latencyMs > 0) "${ch.latencyMs}ms" else "未检测"
        holder.view.setOnClickListener { onClick(ch) }
    }

    override fun getItemCount(): Int = list.size
}

