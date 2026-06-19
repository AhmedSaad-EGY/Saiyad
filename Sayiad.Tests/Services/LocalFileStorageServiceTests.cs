using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Sayiad.Api.Services.FileStorage;

namespace Sayiad.Tests.Services;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(
        Path.GetTempPath(),
        $"sayiad-storage-tests-{Guid.NewGuid():N}");

    [Theory]
    [InlineData("../evil")]
    [InlineData("..\\evil")]
    [InlineData("safe/../evil")]
    public async Task UploadAsync_UnsafeFolderInput_StaysUnderUploadsRoot(string folder)
    {
        var service = CreateService();
        await using var stream = new MemoryStream([1, 2, 3, 4]);

        var url = await service.UploadAsync(stream, "../../avatar.png", folder);

        url.Should().StartWith("/api/images/");
        var parts = url["/api/images/".Length..].Split('/');
        parts.Should().HaveCount(2);

        var savedPath = Path.GetFullPath(Path.Combine(_tempRoot, "uploads", parts[0], parts[1]));
        var uploadsRoot = Path.GetFullPath(Path.Combine(_tempRoot, "uploads"));
        savedPath.Should().StartWith(uploadsRoot + Path.DirectorySeparatorChar);
        File.Exists(savedPath).Should().BeTrue();

        var outsideEvilDir = Path.Combine(Directory.GetParent(_tempRoot)!.FullName, "evil");
        Directory.Exists(outsideEvilDir).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_TraversalUrl_DoesNotDeleteOutsideUploadsRoot()
    {
        Directory.CreateDirectory(_tempRoot);
        var outsideFile = Path.Combine(_tempRoot, "outside.png");
        await File.WriteAllBytesAsync(outsideFile, [1, 2, 3]);

        var service = CreateService();

        await service.DeleteAsync("/api/images/../outside.png");

        File.Exists(outsideFile).Should().BeTrue();
    }

    private LocalFileStorageService CreateService()
    {
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.WebRootPath).Returns(_tempRoot);
        envMock.SetupGet(e => e.ContentRootPath).Returns(_tempRoot);

        return new LocalFileStorageService(envMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
