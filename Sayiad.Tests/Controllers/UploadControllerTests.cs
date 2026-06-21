using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Sayiad.Api.Controllers;
using Sayiad.Api.Filters;
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

    [Fact]
    public async Task Upload_MissingFile_ReturnsBadRequestAndDoesNotStore()
    {
        var result = await CreateController().Upload(null!);

        result.Should().BeOfType<BadRequestObjectResult>();
        _storageMock.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Upload_InvalidExtension_ReturnsBadRequestAndDoesNotStore()
    {
        var file = CreateFormFile(
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            "image.txt",
            "text/plain");

        var result = await CreateController().Upload(file);

        result.Should().BeOfType<BadRequestObjectResult>();
        _storageMock.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Upload_OversizedFile_ReturnsBadRequestAndDoesNotStore()
    {
        var file = new Mock<IFormFile>();
        file.SetupGet(f => f.Length).Returns((5 * 1024 * 1024) + 1);

        var result = await CreateController().Upload(file.Object);

        result.Should().BeOfType<BadRequestObjectResult>();
        _storageMock.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void RequireValidatorFilter_FormFileArgument_SkipsDtoValidatorRequirement()
    {
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();
        var filter = new RequireValidatorFilter(provider, Mock.Of<ILogger<RequireValidatorFilter>>());
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());
        var file = CreateFormFile(
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            "valid.png",
            "image/png");
        var filterContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { ["file"] = file },
            new object());

        filter.OnActionExecuting(filterContext);

        filterContext.Result.Should().BeNull();
        filterContext.ModelState.IsValid.Should().BeTrue();
        filterContext.ModelState.Values
            .SelectMany(value => value.Errors)
            .Should().NotContain(error => error.ErrorMessage.Contains("Validation is not configured"));
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
