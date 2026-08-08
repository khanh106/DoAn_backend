using DoAnV2.Application.Common.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DoAnV2.Infrastructure.Services.Blockchain;

/// <summary>
/// Load file ABI JSON của Smart Contract FruitTraceability từ đường dẫn
/// trong <see cref="BlockchainOptions.AbiPath"/>. Cache trong bộ nhớ.
///
/// Đường dẫn tương đối sẽ được resolve dựa trên ContentRootPath của ứng dụng.
///
/// Xem thêm <c>backend/DoAnV2.API/Contracts/abi.json</c> – file này do người
/// dùng tự compile từ <c>blockchain/FruitTraceability.sol</c> bằng Hardhat/Remix.
/// </summary>
public class AbiLoader
{
    private readonly IHostEnvironment _env;
    private readonly BlockchainOptions _options;
    private string? _cached;
    private readonly object _lock = new();

    public AbiLoader(IHostEnvironment env, IOptions<BlockchainOptions> options)
    {
        _env = env;
        _options = options.Value;
    }

    public string LoadAbi()
    {
        if (_cached != null) return _cached;
        lock (_lock)
        {
            if (_cached != null) return _cached;

            var path = _options.AbiPath;
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Blockchain:AbiPath chưa được cấu hình.");

            var fullPath = Path.IsPathRooted(path)
                ? path
                : Path.Combine(_env.ContentRootPath, path);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException(
                    $"Không tìm thấy file ABI tại '{fullPath}'. " +
                    $"Hãy compile blockchain/FruitTraceability.sol bằng Hardhat và copy file artifacts/contracts/FruitTraceability.sol/FruitTraceability.json ➔ Contracts/abi.json (trích 'abi' field).",
                    fullPath);

            var content = File.ReadAllText(fullPath).Trim();
            if (string.IsNullOrWhiteSpace(content) || content == "{}" || content == "[]")
                throw new InvalidOperationException(
                    $"File ABI rỗng tại '{fullPath}'. Hãy paste JSON ABI thật của FruitTraceability.sol.");

            _cached = content;
            return _cached;
        }
    }
}
