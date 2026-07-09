# FakeHover

FakeHover 是一个为 QQ 音乐准备的小工具。

QQ 音乐在鼠标离开主界面后会隐藏播放进度条和部分悬停状态。FakeHover 会在后台找到 `QQMusic.exe` 的窗口，并定时向窗口发送轻微抖动的 `WM_MOUSEMOVE` 消息，让 QQ 音乐认为鼠标仍然停留在窗口上，从而保持播放进度等悬停界面可见。

## 功能

- 自动查找正在运行的 QQ 音乐窗口
- 向 QQ 音乐窗口模拟鼠标移动消息
- 如果检测到真实鼠标已经在 QQ 音乐窗口内，会暂停本轮模拟，避免干扰实际操作
- 最小化到系统托盘运行
- 托盘菜单支持暂停/恢复模拟和退出程序
- 双击托盘图标可以打开日志窗口

## 使用方式

1. 启动 QQ 音乐。
2. 运行 FakeHover。
3. 程序会自动隐藏到系统托盘并开始工作。
4. 需要暂停时，右键托盘图标，选择“暂停模拟”。
5. 需要退出时，右键托盘图标，选择“退出”。

## 构建环境

- Windows
- .NET 6 SDK
- Visual Studio 2022 或兼容的 .NET 6 Windows Forms 构建环境

命令行构建：

```powershell
dotnet build
```

## 实现说明

程序核心逻辑在 `Form1.cs`：

- 使用 `EnumWindows` 枚举系统窗口
- 通过 `GetWindowThreadProcessId` 找到对应进程
- 匹配 `QQMusic.exe`
- 使用 `GetWindowRect` 获取窗口区域
- 使用 `GetCursorPos` 判断真实鼠标是否已经位于窗口内
- 使用 `PostMessage` 发送 `WM_MOUSEMOVE`

模拟位置会围绕窗口中心加入少量随机偏移，避免每次消息坐标完全相同。

## 注意事项

- 这个工具只针对 QQ 音乐当前窗口行为编写，不保证适用于其他播放器。
- 如果 QQ 音乐后续更新了窗口逻辑，可能需要调整实现。
- 程序通过 Windows 消息模拟悬停效果，不会移动真实鼠标指针。
