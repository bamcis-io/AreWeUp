using Amazon.Lambda.TestUtilities;
using BAMCIS.AreWeUp;
using BAMCIS.AWSLambda.Common.Events;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AreWeUp.Tests
{
    public class AreWeUpTest
    {
        private string _Bucket = "";
        private string _ObjectKey = "areweup.json";

        public AreWeUpTest()
        {
        }

        [Fact]
        public async Task TestUrls()
        {
            // ARRANGE
            CloudWatchScheduledEvent Event = new CloudWatchScheduledEvent(
                "0",
                Guid.Parse("125e7841-c049-462d-86c2-4efa5f64e293"),
                "123456789012",
                DateTime.Parse("2016-12-16T19:55:42Z"),
                "us-east-1",
                new string[] { "arn:aws:events:us-east-1:415720405880:rule/AreWeUp-TestUrls-X2YM3334N4JN" },
                new object()
            );

            TestLambdaLogger TestLogger = new TestLambdaLogger();
            TestClientContext ClientContext = new TestClientContext();
            ClientContext.Environment.Add("ConfigBucket", _Bucket);
            ClientContext.Environment.Add("ConfigKey", _ObjectKey);

            TestLambdaContext Context = new TestLambdaContext()
            {
                FunctionName = "AreWeUp",
                FunctionVersion = "1",
                Logger = TestLogger,
                ClientContext = ClientContext
            };

            LambdaEntryPoint Func = new LambdaEntryPoint();

            // ACT
            await Func.ExecuteHealthChecks(Event, Context);

            // ASSERT
            Assert.True(true);
        }
    }
}
