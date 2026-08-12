using Amazon.S3;
using Amazon.S3.Model;
using DoAnV2.Application.Common.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DoAnV2.API.Controllers;

/// <summary>
/// Proxy đọc file từ Filebase S3 bucket - tránh phụ thuộc vào IPFS public gateway.
/// GET /api/v1/ipfs/{*key}
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/ipfs")]
public class IpfsProxyController : ControllerBase
{
    private readonly IAmazonS3 _s3;
    private readonly IpfsOptions _options;

    public IpfsProxyController(IAmazonS3 s3, IOptions<IpfsOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    [HttpGet("{**key}")]
    public async Task<IActionResult> GetFile(string key, CancellationToken ct)
    {
        try
        {
            var response = await _s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key,
            }, ct);

            return File(response.ResponseStream, response.Headers.ContentType ?? "application/octet-stream");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new { message = $"File không tồn tại: {key}" });
        }
    }
}
