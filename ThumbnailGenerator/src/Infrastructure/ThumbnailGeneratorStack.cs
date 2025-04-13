using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Notifications;
using Constructs;

namespace ThumbnailGenerator
{
    public class ThumbnailGeneratorStack : Stack
    {
        internal ThumbnailGeneratorStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // Create buckets
            var inputBucket = new Bucket(this, "ThumbnailGeneratorInputBucket");
            var outputBucket = new Bucket(this, "ThumbnailGeneratorOutputBucket");

            // Create Lambda
            var lambdaFunction = new Function(this, "ThumbnailGeneratorImageProcessor", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Environment = new Dictionary<string, string>
                {
                    { "OUTPUT_BUCKET", outputBucket.BucketName }
                },
                Handler = "ImageProcessorLambda::ImageProcessorLambda.Function::FunctionHandler",
                Code = Code.FromAsset("src/ImageProcessorLambda/src/ImageProcessorLambda/bin/Debug/net8.0/"),
                Timeout = Duration.Minutes(5)
            });

            // give lambda permissions 
            inputBucket.GrantRead(lambdaFunction);
            outputBucket.GrantWrite(lambdaFunction);
            
            // Add trigger
            inputBucket.AddEventNotification(EventType.OBJECT_CREATED, new LambdaDestination(lambdaFunction));
        }
    }
}
