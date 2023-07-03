using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

var app = WebApplication.Create();


//mapget defining and displaying original upload page
app.MapGet("/", () =>
{
    return Results.Content(@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Image Uploader</title>
                    </head>
                    <body>
                        <div class='form-container'>
                            <h1>Upload Image</h1>
                            <form action='/image' method='post' enctype='multipart/form-data'>
                                <label for='imageTitle'>Title of image:</label>
                                <input type='text' id='imageTitle' name='imageTitle' required>
            
                                <label for='imageFile'>Image file (JPEG, PNG, GIF):</label>
                                <input type='file' id='imageFile' name='imageFile' accept='.jpeg, .png, .gif' required>
            
                                <input type='submit' value='Upload'>
                            </form>
                        </div>
                    </body>
                    </html>
            ", "text/html");
});

app.MapPost("/image", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var title = form["imageTitle"];
    var file = form.Files.GetFile("imageFile");
    var fileExtension = Path.GetExtension(file.FileName).ToLower();

    if (string.IsNullOrEmpty(title) || file == null || file.Length == 0)
    {
        return Results.BadRequest("Invalid request. Title and image file are required.");
    }

    if (fileExtension != ".jpeg" && fileExtension != ".gif" && fileExtension != ".png")
    {
        return Results.BadRequest("Invalid file format. Only .jpeg, .png, and .gif are supported.");
    }

    var imageId = Guid.NewGuid().ToString();
    var imagePath = Path.Combine("pictures", $"{imageId}{Path.GetExtension(file.FileName)}");
    using (var fileStream = new FileStream(imagePath, FileMode.Create))
    {
        await file.CopyToAsync(fileStream);
    }

    var imageDetails = new
    {
        Id = imageId,
        Title = title.ToString(),
        FileName = file.FileName,
        FileExtension = fileExtension
    };

    var jsonData = JsonSerializer.Serialize(imageDetails);
    var jsonPath = "pictures/data.json";
    if (File.Exists(jsonPath))
    {
        File.Delete(jsonPath);
    }
   await File.WriteAllTextAsync(jsonPath, $"{jsonData}{Environment.NewLine}");


    var redirectUrl = $@"/pictures/{imageId}";
    return Results.Redirect(redirectUrl);
});


//mapget to format and upload picture to show after upload to file
app.MapGet("/pictures/{id}", (string id) =>
{
    var jsonData = File.ReadAllText("pictures/data.json");
    var root = getRoot(jsonData);
    string imageId = root.GetProperty("Id").GetString();
    string title = root.GetProperty("Title").GetString();

    if (imageId != id)
    {
        return Results.NotFound("Image not found");
    }

    var htmlContent = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <title>{title}</title>
            <style>
                .image-container {{
                    text-align: left;
                    margin-top: 20px;
                }}

                .image-container img {{
                    max-width: 50%;
                    height: auto;
                }}
            </style>
            </head>
            <body>
                <h1>{title}</h1>
                <div class='image-container'>
                    <img src='/store-image' alt='{title}' />
                </div>
                <script>
                    function redirectBack() {{
                        window.location.href = '/';
                    }}
                </script>
            </body>
            </html>
";
    return Results.Content(htmlContent, "text/html");
});

//mapget to store image into file directory
app.MapGet("/store-image", () =>
{
    var jsonData = File.ReadAllText("pictures/data.json");
    var root = getRoot(jsonData.ToString());
    string imageId = root.GetProperty("Id").GetString();
    string fileExtension = root.GetProperty("FileExtension").GetString();
    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "pictures", $"{imageId}{fileExtension}");

    return Results.File(imagePath, "image/jpeg");
});

JsonElement getRoot(string jsonData)
{
    JsonDocument document = JsonDocument.Parse(jsonData);
    JsonElement root = document.RootElement;
    return root;
}

app.Run();