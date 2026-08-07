# -*- coding: utf-8 -*-
"""
Fix: 把 bottomContainer 从 mainArea 移到 outerWrap 同级，让筛选栏贴到外层底边
（绕开 mainArea.Padding=(16,8,16,8) 的底部 8px 留白）。

变更：
  1. 删掉  mainArea.Controls.Add(bottomContainer);
  2. 在  outerWrap.Controls.Add(bottomBarRef); 之前插入
     outerWrap.Controls.Add(bottomContainer);
     （位于 mainArea 之后、bottomBarRef 之前 -> Dock.Bottom 在最底部，
       bottomBarRef(32px) 被自动推到上方）
"""
import io, sys
p = r'IPTVLiveChecker\IPTVLiveCheckerMain.cs'
s = io.open(p, encoding='utf-8').read()

edits = [
    # 1) 去掉 mainArea.Controls.Add(bottomContainer); 改用 outerWrap 同级
    (
        # 原代码块（mainArea 内挂 bottomContainer）
        'mainArea.Controls.Add(bottomContainer);\n\t\tbottomContainer.Controls.Add(statusBarContainer);\n\t\tCreateFilterBar(bottomContainer);',
        # 替换为：bottomContainer 直接挂 outerWrap（在底部外层）
        'bottomContainer.Controls.Add(statusBarContainer);\n\t\tCreateFilterBar(bottomContainer);',
    ),
]

# 2) 在 outerWrap.Controls.Add 序列里插入 bottomContainer
# 当前顺序：mainArea, bottomBarRef, titleBarPanel
# 期望顺序：mainArea, bottomContainer(在最底部), bottomBarRef, titleBarPanel
outer_search = '\t\touterWrap.Controls.Add(mainArea);\n\t\tCreateBottomBar();\n\t\touterWrap.Controls.Add(bottomBarRef);\n\t\touterWrap.Controls.Add(titleBarPanel);'
outer_replace = '\t\touterWrap.Controls.Add(mainArea);\n\t\tCreateBottomBar();\n\t\touterWrap.Controls.Add(bottomContainer);\n\t\touterWrap.Controls.Add(bottomBarRef);\n\t\touterWrap.Controls.Add(titleBarPanel);'
edits.append((outer_search, outer_replace))

for i, (old, new) in enumerate(edits, 1):
    if old not in s:
        print(f'EDIT #{i}: pattern not found\n---pattern---\n{old[:200]!r}\n---', file=sys.stderr)
        sys.exit(1)
    if s.count(old) != 1:
        print(f'EDIT #{i}: pattern matched {s.count(old)} times (expected 1)', file=sys.stderr)
        sys.exit(1)
    s = s.replace(old, new, 1)
    print(f'EDIT #{i} OK')

io.open(p, 'w', encoding='utf-8').write(s)
print('written')
