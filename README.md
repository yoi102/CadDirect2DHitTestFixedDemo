# CadDirect2DHitTestFixedDemo

这是一个 WinForms + Vortice.Direct2D1 的 CAD 绘制 / 选择 Demo。

重点修复：

1. 绘制使用 Direct2D：`ID2D1HwndRenderTarget` + `ID2D1PathGeometry`。
2. 多边形 Geometry 使用局部 float 坐标缓存，不直接把 1e8 级 CAD 世界坐标塞进 Direct2D。
3. 点击选择不依赖 Direct2D Geometry 的 FillContainsPoint / StrokeContainsPoint。
4. HitTest 使用 double 世界坐标数学判断：`PointInPolygon` + `DistancePointToSegment`。
5. 绘制前根据 double Bounds 判断是否进入视口。
6. 支持滚轮缩放、右键平移、左键点击选择。

## 运行环境

- Windows
- .NET 8 SDK
- Visual Studio 2022 或 `dotnet` CLI

## 运行方式

```bash
dotnet restore
dotnet run --project src/CadDirect2DHitTestFixedDemo/CadDirect2DHitTestFixedDemo.csproj
```

或打开：

```text
CadDirect2DHitTestFixedDemo.sln
```

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

这样可以避免大坐标下 Direct2D float Geometry 参与点击判断导致的偏差。
