namespace Erp.Api.Features.Imports;

public sealed record ImportFile(Stream Content, string? FileName, long Length);
