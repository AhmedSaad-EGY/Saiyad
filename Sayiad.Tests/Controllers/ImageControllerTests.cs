using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sayiad.Api.Controllers;

namespace Sayiad.Tests.Controllers;

public class ImageControllerTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(
        Path.GetTempPath(),
        $"sayiad-image-tests-{Guid.NewGuid():N}");

    [Theory]
    [InlineData("..", "image.png")]
    [InlineData("bad/folder", "image.png")]
    [InlineData("bad\\folder", "image.png")]
    [InlineData("bad:folder", "image.png")]
    [InlineData("profiles", "../image.png")]
    [InlineData("profiles", "bad/image.png")]
    [InlineData("profiles", "bad\\image.png")]
    [InlineData("profiles", "bad:image.png")]
    public void Get_PathTraversalInput_ReturnsBadRequest(string folder, string fileName)
    {
        var controller = CreateController();

        var result = controller.Get(folder, fileName);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Get_ValidGifUnderUploadsRoot_ReturnsPhysicalFileWithGifMime()
    {
        var uploadsDir = Path.Combine(_tempRoot, "uploads", "profiles");
        Directory.CreateDirectory(uploadsDir);
        var filePath = Path.Combine(uploadsDir, "image.gif");
        File.WriteAllBytes(filePath, [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]);

        var result = CreateController().Get("profiles", "image.gif");

        var fileResult = result.Should().BeOfType<PhysicalFileResult>().Subject;
        fileResult.FileName.Should().Be(Path.GetFullPath(filePath));
        fileResult.ContentType.Should().Be("image/gif");
    }

    private ImageController CreateController()
    {
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.WebRootPath).Returns(_tempRoot);
        envMock.SetupGet(e => e.ContentRootPath).Returns(_tempRoot);

        return new ImageController(envMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
