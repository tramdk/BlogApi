using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlogApi.Application.Features.Auth.Commands;
using BlogApi.Domain.Entities;
using Xunit;

namespace BlogApi.Tests.IntegrationTests;

public class FilesControllerTests : BaseIntegrationTest
{
    public FilesControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<string> GetTokenAsync(string email)
    {
        var regCmd = new RegisterCommand(email, "Password123!", "File User");
        await _client.PostAsJsonAsync("/api/auth/register", regCmd);

        var loginCmd = new LoginCommand(email, "Password123!");
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", loginCmd);
        var authRes = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        return authRes!.AccessToken;
    }

    [Fact]
    public async Task UploadAndDownloadFile_Works()
    {
        // Arrange
        var token = await GetTokenAsync("filetest@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(fileContent, "file", "testfile.bin");
        // Add as Form Data, NOT Query String
        content.Add(new StringContent("Test"), "objectType");
        content.Add(new StringContent("123"), "objectId");

        // Act - Upload
        var uploadResp = await _client.PostAsync("/api/files/upload", content);
        
        // Assert - Upload
        Assert.Equal(HttpStatusCode.OK, uploadResp.StatusCode);
        var metadata = await uploadResp.Content.ReadFromJsonAsync<FileMetadata>();
        Assert.NotNull(metadata);
        Assert.Equal("testfile.bin", metadata!.FileName);
        Assert.Equal("Test", metadata.ObjectType);
        Assert.Equal("123", metadata.ObjectId);

        // Act - Download
        var downloadResp = await _client.GetAsync($"/api/files/download/{metadata.Id}");

        // Assert - Download
        Assert.Equal(HttpStatusCode.OK, downloadResp.StatusCode);
        var downloadedBytes = await downloadResp.Content.ReadAsByteArrayAsync();
        Assert.Equal(4, downloadedBytes.Length);
        Assert.Equal(1, downloadedBytes[0]);
    }

    [Fact]
    public async Task DeleteFile_Works()
    {
        // Arrange
        var token = await GetTokenAsync("filedelete@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(fileContent, "file", "testfile_to_delete.bin");
        // Add as Form Data
        content.Add(new StringContent("DeleteTest"), "objectType");
        content.Add(new StringContent("del1"), "objectId");

        var uploadResp = await _client.PostAsync("/api/files/upload", content);
        var metadata = await uploadResp.Content.ReadFromJsonAsync<FileMetadata>();

        // Act
        var deleteResp = await _client.DeleteAsync($"/api/files/{metadata!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        // Verify it's gone
        var downloadResp = await _client.GetAsync($"/api/files/download/{metadata.Id}");
        Assert.Equal(HttpStatusCode.NotFound, downloadResp.StatusCode);
    }

    private record AuthResponse(string AccessToken, string RefreshToken);
}
