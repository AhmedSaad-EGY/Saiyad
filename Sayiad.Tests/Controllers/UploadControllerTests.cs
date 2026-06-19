using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Sayiad.Api.Controllers;
using Sayiad.Domain.Contracts;

namespace Sayiad.Tests.Controllers;

public class UploadControllerTests
{
    private readonly Mock<IFileStorageService> _storageMock = new();
    private readonly Mock<ILogger<UploadController>> _loggerMock = new();

    private UploadController CreateController()
    {
        _storageMock
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("/api/images/sayiad_profiles/test.webp");

        return new UploadController(_storageMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Upload_WebpWithRiffButNoWebpSignature_ReturnsBadRequestAndDoesNotStore()
    {
        var file = CreateFormFile(
            [0x52, 0x49, 0x46, 0x46, 0x08, 0x00, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45],
            "fake.webp",
            "image/webp");

        var result = await CreateController().Upload(file);

        result.Should().BeOfType<BadRequestObjectResult>();
        _storageMock.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Upload_TooSmallWebp_ReturnsBadRequestAndDoesNotStore()
    {
        var file = CreateFormFile(
            [0x52, 0x49, 0x46, 0x46, 0x01],
            "small.webp",
            "image/webp");

        var result = await CreateController().Upload(file);

        result.Should().BeOfType<BadRequestObjectResult>();
        _storageMock.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Upload_ValidWebpSignature_CallsStorage()
    {
        var file = CreateFormFile(
            [0x52, 0x49, 0x46, 0x46, 0x08, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50],
            "valid.webp",
            "image/webp");

        var result = await CreateController().Upload(file);

        result.Should().BeOfType<OkObjectResult>();
        _storageMock.Verify(s => s.UploadAsync(
            It.IsAny<Stream>(),
            It.Is<string>(name => name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)),
            "sayiad/profiles"), Times.Once);
    }

    [Fact]
    public async Task Upload_ValidPngSignature_CallsStorage()
    {
        var file = CreateFormFile(
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            "valid.png",
            "image/png");

        var result = await CreateController().Upload(file);

        result.Should().BeOfType<OkObjectResult>();
        _storageMock.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "sayiad/profiles"), Times.Once);
    }

    private static IFormFile CreateFormFile(byte[] bytes, string fileName, string contentType)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }
}
