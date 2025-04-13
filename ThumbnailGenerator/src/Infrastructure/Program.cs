using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ThumbnailGenerator
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new ThumbnailGeneratorStack(app, "ThumbnailGeneratorStack");
            app.Synth();
        }
    }
}
