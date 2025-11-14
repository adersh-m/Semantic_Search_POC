// Program.cs
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string azureEndpoint = "https://<YOUR_ENDPOINT_NAME>.openai.azure.com/";
string deploymentName = "<YOUR_DEPLOYMENT_NAME>";
string apiKey = "<YOUR_API_KEY>";

// In-memory embeddings storage
var pdfEmbeddings = new Dictionary<string, List<(string Text, float[] Embedding)>>();

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

// ------------------ Upload PDF ------------------
app.MapPost("/upload-pdf", async (HttpRequest request) =>
{
    try
    {
        var form = await request.ReadFormAsync();

        // Try to get file from multiple possible form keys
        var file = form.Files["file"] ?? form.Files.FirstOrDefault();

        if (file == null || file.Length == 0)
            return Results.BadRequest("No file uploaded or file is empty.");

        // Validate file type
        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("Only PDF files are supported.");

        using var stream = file.OpenReadStream();
        using var pdfDoc = new PdfDocument(new PdfReader(stream));
        var texts = new List<string>();

        for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
        {
            var page = pdfDoc.GetPage(i);
            texts.Add(PdfTextExtractor.GetTextFromPage(page));
        }

        var embeddingsList = new List<(string Text, float[] Embedding)>();

        foreach (var text in texts)
        {
            var chunks = SplitTextIntoChunks(text, 500);
            foreach (var chunk in chunks)
            {
                var vector = await GetEmbeddingAsync(chunk);
                embeddingsList.Add((chunk, vector));
            }
        }

        pdfEmbeddings[file.FileName] = embeddingsList;
        return Results.Ok($"Uploaded and processed {file.FileName}. Total chunks: {embeddingsList.Count}");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error processing file: {ex.Message}");
    }
});

// ------------------ Query ------------------
app.MapGet("/query", async (string q) =>
{
    if (!pdfEmbeddings.Any()) return Results.BadRequest("No PDFs processed yet.");

    var queryVector = await GetEmbeddingAsync(q);
    var results = new List<(string PdfName, string Text, float Score)>();

    foreach (var kvp in pdfEmbeddings)
    {
        foreach (var (textChunk, embedding) in kvp.Value)
        {
            float score = CosineSimilarity(queryVector, embedding);
            results.Add((kvp.Key, textChunk, score));
        }
    }

    var top3 = results.OrderByDescending(r => r.Score).Take(3)
    .Select(r => new
    {
        PdfName = r.PdfName,
        Text = r.Text,
        Score = r.Score
    })
    .ToList();
    return Results.Ok(top3);
});

app.Run();

// ------------------ Helper Methods ------------------
static List<string> SplitTextIntoChunks(string text, int wordsPerChunk)
{
    var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var chunks = new List<string>();
    for (int i = 0; i < words.Length; i += wordsPerChunk)
    {
        chunks.Add(string.Join(' ', words.Skip(i).Take(wordsPerChunk)));
    }
    return chunks;
}

static float CosineSimilarity(float[] a, float[] b)
{
    float dot = 0, magA = 0, magB = 0;
    for (int i = 0; i < a.Length; i++)
    {
        dot += a[i] * b[i];
        magA += a[i] * a[i];
        magB += b[i] * b[i];
    }
    return dot / ((float)Math.Sqrt(magA) * (float)Math.Sqrt(magB));
}

// ------------------ REST call to Azure OpenAI Embeddings ------------------
async Task<float[]> GetEmbeddingAsync(string input)
{
    var url = $"{azureEndpoint}/openai/deployments/{deploymentName}/embeddings?api-version=2023-05-15";

    var body = JsonSerializer.Serialize(new { input });
    var content = new StringContent(body, Encoding.UTF8, "application/json");

    var resp = await httpClient.PostAsync(url, content);
    resp.EnsureSuccessStatusCode();

    using var stream = await resp.Content.ReadAsStreamAsync();
    using var doc = await JsonDocument.ParseAsync(stream);

    var embeddingJson = doc.RootElement
        .GetProperty("data")[0]
        .GetProperty("embedding")
        .EnumerateArray()
        .Select(x => x.GetSingle())
        .ToArray();

    return embeddingJson;
}
