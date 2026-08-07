#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import io
import subprocess
import sys

p = r'IPTVLiveChecker\IPTVLiveCheckerMain.cs'

with io.open(p, encoding='utf-8') as f:
    src = f.read()

# ---- 替换 MakeFilterChip 中错误的 lbl.Width 取值 ----
old = (
    "\t\tPanel MakeFilterChip(string text, bool initial, Action<bool> onToggle)\n"
    "\t\t{\n"
    "\t\t\tbool state = initial;\n"
    "\n"
    "\t\t\tvar cc = new Panel\n"
    "\t\t\t{\n"
    "\t\t\t\tSize = new Size(ccSize, ccSize),\n"
    "\t\t\t\tLocation = new Point(0, (SY(30) - ccSize) / 2),\n"
    "\t\t\t\tTag = state\n"
    "\t\t\t};\n"
    "\n"
    "\t\t\tvar lbl = new Label\n"
    "\t\t\t{\n"
    "\t\t\t\tText = text,\n"
    "\t\t\t\tFont = GetFont(SF(9f)),\n"
    "\t\t\t\tAutoSize = true,\n"
    "\t\t\t\tTextAlign = ContentAlignment.MiddleLeft,\n"
    "\t\t\t\tForeColor = state ? theme.TextPrimary : Color.FromArgb(210, theme.TextPrimary)\n"
    "\t\t\t};\n"
    "\t\t\tlbl.Location = new Point(ccSize + SX(4), (SY(30) - lbl.Height) / 2);\n"
    "\n"
    "\t\t\tvar host = new Panel\n"
    "\t\t\t{\n"
    "\t\t\t\tLocation = new Point(x, yMid),\n"
    "\t\t\t\tSize = new Size(ccSize + SX(4) + lbl.Width + SX(4), SY(30)),\n"
    "\t\t\t\tCursor = Cursors.Hand\n"
    "\t\t\t};\n"
)

new = (
    "\t\tPanel MakeFilterChip(string text, bool initial, Action<bool> onToggle)\n"
    "\t\t{\n"
    "\t\t\tbool state = initial;\n"
    "\t\t\tvar font = GetFont(SF(9f));\n"
    "\n"
    "\t\t\t// 精确测量文本宽度（Label.AutoSize 在加入容器前不会同步更新 Width）\n"
    "\t\t\tSize textSize;\n"
    "\t\t\tusing (var g = _filterBar.CreateGraphics())\n"
    "\t\t\t{\n"
    "\t\t\t\ttextSize = TextRenderer.MeasureText(g, text, font);\n"
    "\t\t\t}\n"
    "\t\t\tint labelW = textSize.Width + 2;\n"
    "\t\t\tint labelH = textSize.Height + 2;\n"
    "\n"
    "\t\t\tvar cc = new Panel\n"
    "\t\t\t{\n"
    "\t\t\t\tSize = new Size(ccSize, ccSize),\n"
    "\t\t\t\tLocation = new Point(0, (SY(30) - ccSize) / 2),\n"
    "\t\t\t\tTag = state\n"
    "\t\t\t};\n"
    "\n"
    "\t\t\tvar lbl = new Label\n"
    "\t\t\t{\n"
    "\t\t\t\tText = text,\n"
    "\t\t\t\tFont = font,\n"
    "\t\t\t\tAutoSize = false,\n"
    "\t\t\t\tSize = new Size(labelW, labelH),\n"
    "\t\t\t\tTextAlign = ContentAlignment.MiddleLeft,\n"
    "\t\t\t\tForeColor = state ? theme.TextPrimary : Color.FromArgb(210, theme.TextPrimary)\n"
    "\t\t\t};\n"
    "\t\t\tlbl.Location = new Point(ccSize + SX(4), (SY(30) - labelH) / 2);\n"
    "\n"
    "\t\t\tvar host = new Panel\n"
    "\t\t\t{\n"
    "\t\t\t\tLocation = new Point(x, yMid),\n"
    "\t\t\t\tSize = new Size(ccSize + SX(4) + labelW + SX(4), SY(30)),\n"
    "\t\t\t\tCursor = Cursors.Hand\n"
    "\t\t\t};\n"
)

if old not in src:
    print("ERROR: old block not found", file=sys.stderr)
    sys.exit(1)

n = src.count(old)
if n != 1:
    print(f"ERROR: old block matches {n} times", file=sys.stderr)
    sys.exit(1)

src = src.replace(old, new, 1)

with io.open(p, 'w', encoding='utf-8') as f:
    f.write(src)

print("OK: MakeFilterChip updated with explicit TextRenderer.MeasureText")
