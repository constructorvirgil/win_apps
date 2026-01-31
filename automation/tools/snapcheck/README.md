# snapcheck

用于验证“全屏截屏（虚拟屏幕）”功能稳定性的 CLI。

## 为什么需要它

截图本身（抓屏 + 编码）通常很稳定；真正容易失败的是**写剪贴板**：当某些应用（剪贴板管理器、远程桌面、IM、输入法等）短时间占用剪贴板时，Windows 会返回 `CLIPBRD_E_CANT_OPEN`，导致写入失败。

本项目将“截图抓取/编码”与“写剪贴板”分开验证：

- `selftest`：反复抓屏 + JPEG 编码 + 解码回读（稳定性核心）
- `clipboard`：额外验证写剪贴板与回读（可能被外部程序干扰）

## 用法

抓屏并保存为 JPEG：

```powershell
dotnet run --project .\tools\snapcheck\snapcheck.csproj -c Debug -- capture --out .\.ai\screenshot.jpg --cursor 1 --quality 85 --timing 1
```

稳定性自测（只测抓屏/编码，推荐在 CI 或日常回归用）：

```powershell
dotnet run --project .\tools\snapcheck\snapcheck.csproj -c Debug -- selftest --count 10 --cursor 1 --clipboard 0 --timing 1
```

SLA 检查：保证“截图操作”每次都在 1 秒内返回（剪贴板写入尝试超时后会快速落盘）：

```powershell
dotnet run --project .\tools\snapcheck\snapcheck.csproj -c Debug -- check --count 10 --cursor 1 --maxms 1000 --clipboard 1 --clipms 400 --keep 0
```

剪贴板写入测试（若失败，先关闭/暂停剪贴板管理器再试）：

```powershell
dotnet run --project .\tools\snapcheck\snapcheck.csproj -c Debug -- clipboard --cursor 1
```
