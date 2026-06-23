<img width="1186" height="793" alt="image" src="https://github.com/user-attachments/assets/e31c2316-c4a1-4ce0-97cf-7755e0b370f0" />

## CadDirect2DHitTestFixedDemo

这是一个 WinForms + Vortice.Direct2D1 的 CAD 绘制 / 选择 Demo。




## 操作

- 鼠标滚轮：缩放
- 右键拖动：平移
- 左键点击：选择多边形

## 核心思路

```text
CAD 世界坐标：double
Bounds：double
HitTest：double 数学判断
Direct2D Geometry：局部 float 坐标，只负责绘制
```

