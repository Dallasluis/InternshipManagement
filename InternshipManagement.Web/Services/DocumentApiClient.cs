using System.Net.Http.Headers;
using System.Net.Http.Json;
using InternshipManagement.Web.Models.Documents;

namespace InternshipManagement.Web.Services
{
    public sealed class DocumentApiClient : IDocumentApiClient
    {
        private readonly HttpClient _httpClient;

        public DocumentApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("InternshipApi");
        }

        public async Task<PrivateDocumentResponse> UploadAsync(
            string token,
            IFormFile file,
            string category,
            CancellationToken cancellationToken = default)
        {
            AddAuthorization(token);
            await using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            using var form = new MultipartFormDataContent
            {
                { new StringContent(category), "Category" },
                { fileContent, "File", Path.GetFileName(file.FileName) }
            };
            using var response = await _httpClient.PostAsync("api/documents", form, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(await response.Content.ReadAsStringAsync(cancellationToken));

            return await response.Content.ReadFromJsonAsync<PrivateDocumentResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("The document API returned an empty response.");
        }

        public async Task<IReadOnlyList<PrivateDocumentResponse>> GetOwnedDocumentsAsync(
            string token,
            CancellationToken cancellationToken = default)
        {
            AddAuthorization(token);
            using var response = await _httpClient.GetAsync("api/documents", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PrivateDocumentResponse>>(cancellationToken: cancellationToken)
                ?? new List<PrivateDocumentResponse>();
        }

        public async Task<DocumentAccessLinkResponse?> CreateAccessLinkAsync(
            string token,
            Guid documentId,
            int? reportId = null,
            int? companyProfileId = null,
            CancellationToken cancellationToken = default)
        {
            AddAuthorization(token);
            var queryItems = new List<string>();
            if (reportId.HasValue)
                queryItems.Add($"reportId={reportId.Value}");
            if (companyProfileId.HasValue)
                queryItems.Add($"companyProfileId={companyProfileId.Value}");
            var query = queryItems.Count == 0 ? string.Empty : $"?{string.Join("&", queryItems)}";
            using var response = await _httpClient.PostAsync(
                $"api/documents/{documentId}/access-link{query}", null, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<DocumentAccessLinkResponse>(cancellationToken: cancellationToken);
        }

        public async Task<bool> SetRetentionStateAsync(
            string token,
            Guid documentId,
            string state,
            CancellationToken cancellationToken = default)
        {
            AddAuthorization(token);
            using var response = await _httpClient.PutAsJsonAsync(
                $"api/documents/{documentId}/retention", new { State = state }, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        public async Task<DocumentFileResponse?> DownloadAsync(
            string token,
            Guid documentId,
            int? reportId = null,
            int? companyProfileId = null,
            CancellationToken cancellationToken = default)
        {
            var link = await CreateAccessLinkAsync(token, documentId, reportId, companyProfileId, cancellationToken);
            if (link == null)
                return null;

            AddAuthorization(token);
            var uri = new Uri(_httpClient.BaseAddress!, link.Url);
            using var response = await _httpClient.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var disposition = response.Content.Headers.ContentDisposition;
            return new DocumentFileResponse
            {
                Content = await response.Content.ReadAsByteArrayAsync(cancellationToken),
                ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                FileName = disposition?.FileNameStar ?? disposition?.FileName?.Trim('"') ?? "document"
            };
        }

        private void AddAuthorization(string token) =>
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
