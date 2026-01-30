using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Principal;

namespace VirtualHidSimulator.Services;

/// <summary>
/// Interception 驱动自动安装器
/// 自动下载、解压、安装 Interception 内核驱动
/// </summary>
public class InterceptionInstaller
{
    // Interception 驱动下载地址
    private const string DownloadUrl = "https://github.com/oblitum/Interception/releases/download/v1.0.1/Interception.zip";
    private const string DllName = "interception.dll";
    private const string InstallerName = "install-interception.exe";

    /// <summary>
    /// 安装进度事件
    /// </summary>
    public event Action<string>? ProgressChanged;

    /// <summary>
    /// 安装状态
    /// </summary>
    public enum InstallStatus
    {
        Success,
        NeedRestart,
        AlreadyInstalled,
        DownloadFailed,
        ExtractFailed,
        InstallFailed,
        NeedAdminPrivilege,
        Cancelled
    }

    /// <summary>
    /// 安装结果
    /// </summary>
    public class InstallResult
    {
        public InstallStatus Status { get; set; }
        public string Message { get; set; } = "";
        public bool NeedRestart { get; set; }
    }

    /// <summary>
    /// 检查是否以管理员权限运行
    /// </summary>
    public static bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// 以管理员权限重新启动程序
    /// </summary>
    public static bool RestartAsAdmin()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName,
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查驱动是否已安装（检查系统驱动目录）
    /// </summary>
    public static bool IsDriverInstalled()
    {
        // 检查驱动服务是否存在
        string driverPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "drivers",
            "keyboard.sys");

        // 另一种方式：检查注册表中的驱动服务
        // 这里简化处理，通过检查 DLL 是否存在于程序目录
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string dllPath = Path.Combine(appDir, DllName);
        
        return File.Exists(dllPath);
    }

    /// <summary>
    /// 检查 DLL 是否已存在于程序目录
    /// </summary>
    public static bool IsDllPresent()
    {
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string dllPath = Path.Combine(appDir, DllName);
        return File.Exists(dllPath);
    }

    /// <summary>
    /// 执行完整的安装流程
    /// </summary>
    public async Task<InstallResult> InstallAsync(CancellationToken cancellationToken = default)
    {
        // 检查管理员权限
        if (!IsRunningAsAdmin())
        {
            return new InstallResult
            {
                Status = InstallStatus.NeedAdminPrivilege,
                Message = "安装驱动需要管理员权限，请以管理员身份运行程序。"
            };
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "InterceptionInstall_" + Guid.NewGuid().ToString("N")[..8]);
        string zipPath = Path.Combine(tempDir, "Interception.zip");
        string extractDir = Path.Combine(tempDir, "extracted");

        try
        {
            // 创建临时目录
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(extractDir);

            // 步骤1：下载驱动包
            ReportProgress("正在下载 Interception 驱动...");
            if (!await DownloadFileAsync(DownloadUrl, zipPath, cancellationToken))
            {
                return new InstallResult
                {
                    Status = InstallStatus.DownloadFailed,
                    Message = "下载驱动包失败，请检查网络连接或手动下载。"
                };
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 步骤2：解压文件
            ReportProgress("正在解压文件...");
            if (!ExtractZip(zipPath, extractDir))
            {
                return new InstallResult
                {
                    Status = InstallStatus.ExtractFailed,
                    Message = "解压驱动包失败。"
                };
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 步骤3：查找安装程序和 DLL
            string? installerPath = FindFile(extractDir, InstallerName);
            string? dllPath = FindDll(extractDir);

            if (installerPath == null)
            {
                return new InstallResult
                {
                    Status = InstallStatus.ExtractFailed,
                    Message = $"未找到安装程序 {InstallerName}。"
                };
            }

            // 步骤4：执行安装程序
            ReportProgress("正在安装驱动（需要管理员权限）...");
            var installResult = await RunInstallerAsync(installerPath, cancellationToken);
            
            if (!installResult.success)
            {
                return new InstallResult
                {
                    Status = InstallStatus.InstallFailed,
                    Message = $"安装驱动失败：{installResult.message}"
                };
            }

            // 步骤5：复制 DLL 到程序目录
            if (dllPath != null)
            {
                ReportProgress("正在复制 DLL 文件...");
                CopyDllToAppDirectory(dllPath);
            }

            ReportProgress("安装完成！需要重启电脑才能生效。");
            
            return new InstallResult
            {
                Status = InstallStatus.NeedRestart,
                Message = "Interception 驱动安装成功！请重启电脑使驱动生效。",
                NeedRestart = true
            };
        }
        catch (OperationCanceledException)
        {
            return new InstallResult
            {
                Status = InstallStatus.Cancelled,
                Message = "安装已取消。"
            };
        }
        catch (Exception ex)
        {
            return new InstallResult
            {
                Status = InstallStatus.InstallFailed,
                Message = $"安装过程中发生错误：{ex.Message}"
            };
        }
        finally
        {
            // 清理临时文件
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }

    /// <summary>
    /// 仅复制 DLL（如果驱动已安装但 DLL 不存在）
    /// </summary>
    public async Task<InstallResult> CopyDllOnlyAsync(CancellationToken cancellationToken = default)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "InterceptionDll_" + Guid.NewGuid().ToString("N")[..8]);
        string zipPath = Path.Combine(tempDir, "Interception.zip");
        string extractDir = Path.Combine(tempDir, "extracted");

        try
        {
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(extractDir);

            ReportProgress("正在下载 DLL 文件...");
            if (!await DownloadFileAsync(DownloadUrl, zipPath, cancellationToken))
            {
                return new InstallResult
                {
                    Status = InstallStatus.DownloadFailed,
                    Message = "下载失败。"
                };
            }

            ReportProgress("正在解压...");
            if (!ExtractZip(zipPath, extractDir))
            {
                return new InstallResult
                {
                    Status = InstallStatus.ExtractFailed,
                    Message = "解压失败。"
                };
            }

            string? dllPath = FindDll(extractDir);
            if (dllPath == null)
            {
                return new InstallResult
                {
                    Status = InstallStatus.ExtractFailed,
                    Message = "未找到 DLL 文件。"
                };
            }

            ReportProgress("正在复制 DLL...");
            CopyDllToAppDirectory(dllPath);

            return new InstallResult
            {
                Status = InstallStatus.Success,
                Message = "DLL 文件已复制成功。"
            };
        }
        catch (OperationCanceledException)
        {
            return new InstallResult
            {
                Status = InstallStatus.Cancelled,
                Message = "操作已取消。"
            };
        }
        catch (Exception ex)
        {
            return new InstallResult
            {
                Status = InstallStatus.InstallFailed,
                Message = $"复制 DLL 失败：{ex.Message}"
            };
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    private async Task<bool> DownloadFileAsync(string url, string targetPath, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    int progress = (int)((totalRead * 100) / totalBytes);
                    ReportProgress($"下载中... {progress}% ({totalRead / 1024}KB / {totalBytes / 1024}KB)");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            ReportProgress($"下载失败：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 解压 ZIP 文件
    /// </summary>
    private bool ExtractZip(string zipPath, string extractDir)
    {
        try
        {
            ZipFile.ExtractToDirectory(zipPath, extractDir, true);
            return true;
        }
        catch (Exception ex)
        {
            ReportProgress($"解压失败：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 递归查找文件
    /// </summary>
    private static string? FindFile(string directory, string fileName)
    {
        try
        {
            var files = Directory.GetFiles(directory, fileName, SearchOption.AllDirectories);
            return files.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 查找适合当前系统架构的 DLL
    /// </summary>
    private static string? FindDll(string directory)
    {
        try
        {
            // 优先查找与当前进程架构匹配的 DLL
            string arch = Environment.Is64BitProcess ? "x64" : "x86";
            
            // 查找 library/x64 或 library/x86 目录下的 DLL
            var archDll = Directory.GetFiles(directory, DllName, SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains(arch, StringComparison.OrdinalIgnoreCase));
            
            if (archDll != null)
                return archDll;

            // 如果没找到架构特定的，返回任意找到的
            return Directory.GetFiles(directory, DllName, SearchOption.AllDirectories).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 运行安装程序
    /// </summary>
    private async Task<(bool success, string message)> RunInstallerAsync(string installerPath, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/install",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas"  // 请求管理员权限
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            // 等待进程完成
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            // 等待进程退出（最多等待60秒）
            var completed = await Task.Run(() => process.WaitForExit(60000), cancellationToken);
            
            if (!completed)
            {
                try { process.Kill(); } catch { }
                return (false, "安装程序超时未响应。");
            }

            var output = await outputTask;
            var error = await errorTask;

            // 检查退出码
            if (process.ExitCode == 0)
            {
                return (true, "安装成功。");
            }
            else
            {
                string errorMsg = !string.IsNullOrEmpty(error) ? error : output;
                return (false, $"安装程序返回错误码 {process.ExitCode}：{errorMsg}");
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 740)
        {
            // 需要提升权限
            return (false, "需要管理员权限才能安装驱动。");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 复制 DLL 到程序目录
    /// </summary>
    private void CopyDllToAppDirectory(string sourceDllPath)
    {
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string targetPath = Path.Combine(appDir, DllName);

        try
        {
            File.Copy(sourceDllPath, targetPath, overwrite: true);
            ReportProgress($"已复制 {DllName} 到程序目录。");
        }
        catch (Exception ex)
        {
            ReportProgress($"复制 DLL 失败：{ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 报告进度
    /// </summary>
    private void ReportProgress(string message)
    {
        ProgressChanged?.Invoke(message);
    }

    /// <summary>
    /// 卸载驱动
    /// </summary>
    public async Task<InstallResult> UninstallAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunningAsAdmin())
        {
            return new InstallResult
            {
                Status = InstallStatus.NeedAdminPrivilege,
                Message = "卸载驱动需要管理员权限。"
            };
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "InterceptionUninstall_" + Guid.NewGuid().ToString("N")[..8]);
        string zipPath = Path.Combine(tempDir, "Interception.zip");
        string extractDir = Path.Combine(tempDir, "extracted");

        try
        {
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(extractDir);

            ReportProgress("正在下载卸载程序...");
            if (!await DownloadFileAsync(DownloadUrl, zipPath, cancellationToken))
            {
                return new InstallResult
                {
                    Status = InstallStatus.DownloadFailed,
                    Message = "下载失败。"
                };
            }

            ReportProgress("正在解压...");
            if (!ExtractZip(zipPath, extractDir))
            {
                return new InstallResult
                {
                    Status = InstallStatus.ExtractFailed,
                    Message = "解压失败。"
                };
            }

            string? installerPath = FindFile(extractDir, InstallerName);
            if (installerPath == null)
            {
                return new InstallResult
                {
                    Status = InstallStatus.ExtractFailed,
                    Message = "未找到卸载程序。"
                };
            }

            ReportProgress("正在卸载驱动...");
            var startInfo = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/uninstall",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            await Task.Run(() => process.WaitForExit(60000), cancellationToken);

            // 删除程序目录下的 DLL
            string appDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DllName);
            if (File.Exists(appDll))
            {
                try { File.Delete(appDll); } catch { }
            }

            return new InstallResult
            {
                Status = InstallStatus.NeedRestart,
                Message = "驱动已卸载，请重启电脑。",
                NeedRestart = true
            };
        }
        catch (OperationCanceledException)
        {
            return new InstallResult
            {
                Status = InstallStatus.Cancelled,
                Message = "操作已取消。"
            };
        }
        catch (Exception ex)
        {
            return new InstallResult
            {
                Status = InstallStatus.InstallFailed,
                Message = $"卸载失败：{ex.Message}"
            };
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch { }
        }
    }
}
