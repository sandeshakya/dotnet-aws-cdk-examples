using System.Net.Mime;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ImageProcessorLambda;

public class Function
{
    IAmazonS3 S3Client { get; set; }

    public Function()
    {
        S3Client = new AmazonS3Client();
    }

    private (int width, int height) GetScaledSize(int originalWidth, int originalHeight, int maxDimension)
    {
        float ratio = Math.Min((float)maxDimension / originalWidth, (float)maxDimension / originalHeight);
        return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
    }

    public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        var eventRecords = evnt.Records ?? new List<S3Event.S3EventNotificationRecord>();
        foreach (var record in eventRecords)
        {
            var s3Event = record.S3;
            if (s3Event == null)
            {
                continue;
            }

            try
            {
                var inputBucket = s3Event.Bucket.Name;
                var key = s3Event.Object.Key;
                var outputBucket = Environment.GetEnvironmentVariable("OUTPUT_BUCKET");
                Console.WriteLine($"incoming file {inputBucket} - {key}");
                Console.WriteLine($"output bucket {outputBucket}");
                var response = await S3Client.GetObjectAsync(inputBucket, key);
                if (!response.Headers.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"{key} is not an image");
                    return;
                }
                Console.WriteLine(response.Headers.ContentType);
                using var originalStream = response.ResponseStream;
                
                Console.WriteLine("Resizing image...");
                using var image = await Image.LoadAsync(originalStream);
                image.Mutate(x => x.Resize(150, 0)); // 150px wide, maintain aspect ratio
                
                // Save to memory stream
                using var outputStream = new MemoryStream();
                await image.SaveAsJpegAsync(outputStream);
                outputStream.Position = 0;
                
                Console.WriteLine("Saving thumbnail...");
                
                await S3Client.PutObjectAsync(new PutObjectRequest()
                {
                    BucketName = outputBucket,
                    Key = key,
                    InputStream = outputStream,
                    ContentType = "image/jpeg"
                });
                
                Console.WriteLine($"Thumbnail added to {outputBucket}/{key}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }
    }
}