namespace VirtualHidSimulator.Core.Drivers;

/// <summary>
/// 驱动类型枚举
/// </summary>
public enum DriverType
{
    /// <summary>
    /// SendInput API (用户模式，无需安装)
    /// </summary>
    SendInput,

    /// <summary>
    /// Interception 驱动 (内核模式，需要安装驱动)
    /// </summary>
    Interception
}

/// <summary>
/// 输入驱动管理器
/// 负责管理和切换不同的输入驱动
/// </summary>
public class DriverManager : IDisposable
{
    private static DriverManager? _instance;
    private static readonly object _lock = new();

    private IInputDriver? _currentDriver;
    private DriverType _currentDriverType = DriverType.SendInput;
    private readonly Dictionary<DriverType, IInputDriver> _drivers = new();

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static DriverManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new DriverManager();
                }
            }
            return _instance;
        }
    }

    private DriverManager()
    {
        // 注册可用的驱动
        RegisterDriver(DriverType.SendInput, new SendInputDriver());
        RegisterDriver(DriverType.Interception, new InterceptionDriver());

        // 默认使用 SendInput
        SetDriver(DriverType.SendInput);
    }

    /// <summary>
    /// 注册驱动
    /// </summary>
    private void RegisterDriver(DriverType type, IInputDriver driver)
    {
        _drivers[type] = driver;
    }

    /// <summary>
    /// 当前使用的驱动
    /// </summary>
    public IInputDriver CurrentDriver => _currentDriver!;

    /// <summary>
    /// 当前驱动类型
    /// </summary>
    public DriverType CurrentDriverType => _currentDriverType;

    /// <summary>
    /// 获取所有可用的驱动信息
    /// </summary>
    public IEnumerable<DriverInfo> GetAvailableDrivers()
    {
        foreach (var kvp in _drivers)
        {
            var driver = kvp.Value;
            bool initialized = driver.Initialize();
            
            yield return new DriverInfo
            {
                Type = kvp.Key,
                Name = driver.Name,
                Description = driver.Description,
                IsAvailable = driver.IsAvailable,
                SupportsLockScreen = driver.SupportsLockScreen,
                IsCurrent = kvp.Key == _currentDriverType
            };
        }
    }

    /// <summary>
    /// 切换驱动
    /// </summary>
    public bool SetDriver(DriverType type)
    {
        if (!_drivers.TryGetValue(type, out var driver))
        {
            return false;
        }

        // 初始化驱动
        if (!driver.Initialize())
        {
            // 如果初始化失败且当前没有驱动，使用 SendInput 作为后备
            if (_currentDriver == null && type != DriverType.SendInput)
            {
                return SetDriver(DriverType.SendInput);
            }
            return false;
        }

        _currentDriver = driver;
        _currentDriverType = type;
        return true;
    }

    /// <summary>
    /// 尝试使用最佳可用驱动（优先使用支持锁屏的驱动）
    /// </summary>
    public DriverType UseBestAvailableDriver()
    {
        // 优先尝试 Interception（支持锁屏）
        if (SetDriver(DriverType.Interception))
        {
            return DriverType.Interception;
        }

        // 回退到 SendInput
        SetDriver(DriverType.SendInput);
        return DriverType.SendInput;
    }

    /// <summary>
    /// 检查指定驱动是否可用
    /// </summary>
    public bool IsDriverAvailable(DriverType type)
    {
        if (_drivers.TryGetValue(type, out var driver))
        {
            driver.Initialize();
            return driver.IsAvailable;
        }
        return false;
    }

    public void Dispose()
    {
        foreach (var driver in _drivers.Values)
        {
            driver.Dispose();
        }
        _drivers.Clear();
        _currentDriver = null;
    }
}

/// <summary>
/// 驱动信息
/// </summary>
public class DriverInfo
{
    public DriverType Type { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsAvailable { get; set; }
    public bool SupportsLockScreen { get; set; }
    public bool IsCurrent { get; set; }
}
