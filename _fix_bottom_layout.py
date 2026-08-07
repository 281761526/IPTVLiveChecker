#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import io
import sys

p = r'IPTVLiveChecker\IPTVLiveCheckerMain.cs'

with io.open(p, encoding='utf-8') as f:
    src = f.read()

replacements = []

# 1) 筛选栏高度 SY(38) -> SY(34)，芯片 SY(30) -> SY(26)（多处）
replacements.append(
    ("\t\t\tHeight = SY(38),", "\t\t\tHeight = SY(34),")
)
replacements.append(
    ("\t\tint barH = SY(38);", "\t\tint barH = SY(34);")
)
# 4 处 SY(30) -> SY(26)
replacements.append(
    ("\t\tint yMid = (barH - SY(30)) / 2;", "\t\tint yMid = (barH - SY(26)) / 2;")
)
replacements.append(
    ("\t\t\t\tLocation = new Point(0, (SY(30) - ccSize) / 2),",
     "\t\t\t\tLocation = new Point(0, (SY(26) - ccSize) / 2),")
)
replacements.append(
    ("\t\t\tlbl.Location = new Point(ccSize + SX(4), (SY(30) - labelH) / 2);",
     "\t\t\tlbl.Location = new Point(ccSize + SX(4), (SY(26) - labelH) / 2);")
)
replacements.append(
    ("\t\t\t\tSize = new Size(ccSize + SX(4) + labelW + SX(4), SY(30)),",
     "\t\t\t\tSize = new Size(ccSize + SX(4) + labelW + SX(4), SY(26)),")
)

# 2) CreateFilterBar 签名：接收底部容器参数
replacements.append(
    ("\tprivate void CreateFilterBar()", "\tprivate void CreateFilterBar(Panel bottomContainer)")
)

# 3) 把 _filterBar 加入底部容器（而非 mainArea）
replacements.append(
    ("\t\tmainArea.Controls.Add(_filterBar);", "\t\tbottomContainer.Controls.Add(_filterBar);")
)

# 4) 挂载处：引入 bottomContainer，状态栏在上、筛选栏在下
old_mount = (
    "\t\tmainArea.Controls.Add(gridContainerRef);\n"
    "\t\tCreateFilterBar();\n"
    "\t\tmainArea.Controls.Add(statusBarContainer);\n"
)
new_mount = (
    "\t\tmainArea.Controls.Add(gridContainerRef);\n"
    "\n"
    "\t\t// 底部容器：状态栏在上、筛选栏在下，整体停靠底部（筛选栏位于最底部）\n"
    "\t\tvar bottomContainer = new Panel\n"
    "\t\t{\n"
    "\t\t\tDock = DockStyle.Bottom,\n"
    "\t\t\tHeight = SY(32) + SY(34),\n"
    "\t\t\tBackColor = theme.Bg\n"
    "\t\t};\n"
    "\t\tmainArea.Controls.Add(bottomContainer);\n"
    "\t\tbottomContainer.Controls.Add(statusBarContainer);\n"
    "\t\tCreateFilterBar(bottomContainer);\n"
)
replacements.append((old_mount, new_mount))

# 校验并执行
for old, new in replacements:
    cnt = src.count(old)
    if cnt != 1:
        print(f"ERROR: expected 1 occurrence, found {cnt} for:\n{old!r}", file=sys.stderr)
        sys.exit(1)

for old, new in replacements:
    src = src.replace(old, new, 1)

with io.open(p, 'w', encoding='utf-8') as f:
    f.write(src)

print("OK: bottom layout restructured (filter bar at very bottom, height compacted SY38->SY34)")
