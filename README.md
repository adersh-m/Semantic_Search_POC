# Semantic Search POC API

A .NET 9 Web API that demonstrates semantic search capabilities using Azure OpenAI embeddings and PDF document processing.

## Features

- **PDF Upload & Processing**: Upload PDF files and automatically extract text content
- **Text Chunking**: Splits extracted text into manageable chunks for better embedding generation
- **Vector Embeddings**: Generates embeddings using Azure OpenAI's text-embedding model
- **Semantic Search**: Query uploaded documents using natural language with cosine similarity matching
- **In-Memory Storage**: Stores embeddings in memory for fast retrieval during the session

## Prerequisites

- .NET 9 SDK
- Azure OpenAI Service access with text embedding deployment
- Visual Studio 2022 or VS Code

## Setup

1. **Clone the repository**
   ```bash
   git clone <your-repo-url>
   cd Semantic_Search_POC
   ```

2. **Configure Azure OpenAI credentials**
   
   Update the following variables in `Program.cs`:
   ```csharp
   string azureEndpoint = "https://<YOUR_ENDPOINT_NAME>.openai.azure.com/";
   string deploymentName = "<YOUR_DEPLOYMENT_NAME>";
   string apiKey = "<YOUR_API_KEY>";
   ```

3. **Install dependencies**
   ```bash
   cd SeamnticSeachPOCApi
   dotnet restore
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

The API will be available at `https://localhost:5001` (or the port shown in the console).

## API Endpoints

### Upload PDF
```
POST /upload-pdf
Content-Type: multipart/form-data

Form Data:
- file: PDF file to upload
```

**Response:**
```json
"Uploaded and processed filename.pdf. Total chunks: 25"
```

### Search Documents
```
GET /query?q=your search query
```

**Response:**
```json
[
  {
    "PdfName": "document.pdf",
    "Text": "relevant text chunk...",
    "Score": 0.85
  },
  {
    "PdfName": "document.pdf", 
    "Text": "another relevant chunk...",
    "Score": 0.82
  }
]
```

## Testing the API

### Using curl

1. **Upload a PDF:**
   ```bash
   curl -X POST -F "file=@sample.pdf" http://localhost:5000/upload-pdf
   ```

2. **Search documents:**
   ```bash
   curl "http://localhost:5000/query?q=machine learning"
   ```

### Using PowerShell

1. **Upload a PDF:**
   ```powershell
   $uri = "http://localhost:5000/upload-pdf"
   $filePath = "C:\path\to\your\document.pdf"
   $form = @{
       file = Get-Item -Path $filePath
   }
   Invoke-RestMethod -Uri $uri -Method Post -Form $form
   ```

2. **Search documents:**
   ```powershell
   Invoke-RestMethod -Uri "http://localhost:5000/query?q=artificial intelligence"
   ```

### Using Postman

1. **Upload PDF:**
   - Method: POST
   - URL: `http://localhost:5000/upload-pdf`
   - Body: form-data
   - Key: `file`, Value: Select PDF file

2. **Search:**
   - Method: GET
   - URL: `http://localhost:5000/query?q=your search term`

## How It Works

1. **PDF Processing**: When a PDF is uploaded, the API extracts text from each page using iText7
2. **Text Chunking**: The extracted text is split into chunks of approximately 500 words each
3. **Embedding Generation**: Each chunk is sent to Azure OpenAI to generate vector embeddings
4. **Storage**: Embeddings are stored in memory with their associated text chunks
5. **Search**: When querying, the search term is converted to an embedding and compared against stored embeddings using cosine similarity
6. **Results**: The top 3 most similar chunks are returned with their similarity scores

## Configuration

### Chunk Size
Modify the chunk size by changing the parameter in `SplitTextIntoChunks()`:
```csharp
var chunks = SplitTextIntoChunks(text, 500); // 500 words per chunk
```

### Result Count
Change the number of results returned:
```csharp
var top3 = results.OrderByDescending(r => r.Score).Take(3); // Top 3 results
```

## Limitations

- **In-Memory Storage**: Embeddings are lost when the application restarts
- **File Size**: Large PDFs may take time to process
- **Concurrent Uploads**: Multiple simultaneous uploads may impact performance
- **API Rate Limits**: Subject to Azure OpenAI rate limits

## Future Enhancements

- [ ] Persistent storage (database integration)
- [ ] Authentication and authorization
- [ ] Support for additional file formats (Word, TXT)
- [ ] Batch processing for multiple files
- [ ] Caching mechanisms
- [ ] Health check endpoint improvements
- [ ] Logging and monitoring
- [ ] Docker containerization

## Dependencies

- **itext7**: PDF text extraction
- **System.Text.Json**: JSON serialization
- **Microsoft.AspNetCore.App**: Web API framework

## License

This project is for demonstration purposes. Please ensure compliance with Azure OpenAI usage policies and terms of service.

## Troubleshooting

### Common Issues

1. **Azure OpenAI Authentication Error**
   - Verify your API key, endpoint, and deployment name
   - Check Azure OpenAI service status

2. **PDF Processing Error**
   - Ensure the uploaded file is a valid PDF
   - Check file size limits

3. **No Results Found**
   - Verify PDFs have been uploaded and processed
   - Try different search terms
   - Check if embeddings were generated successfully

### Debug Tips

- Check the console output for detailed error messages
- Verify network connectivity to Azure OpenAI endpoint
- Test with simple PDF files first