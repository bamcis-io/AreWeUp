using Amazon.Lambda.Core;
using BAMCIS.AreWeUp.Models;
using BAMCIS.AWSLambda.Common;
using BAMCIS.AWSLambda.Common.Events;
using System;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
namespace BAMCIS.AreWeUp
{
    /// <summary>
    /// Contains the entry point for the Lambda function
    /// </summary>
    public class LambdaEntryPoint
    {
        #region Private Fields

        /// <summary>
        /// The Lambda context so all methods can write to the log
        /// </summary>
        private ILambdaContext _Context;

        /// <summary>
        /// The AreWeUpClient that will actually execute the checks
        /// </summary>
        private AreWeUpClient _Client;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor that Lambda will invoke. This sets up the AreWeUpClient.
        /// </summary>
        public LambdaEntryPoint()
        {
            string Bucket = Environment.GetEnvironmentVariable("ConfigBucket");
            string Key = Environment.GetEnvironmentVariable("ConfigKey");

            AreWeUpClientConfig ClientConfig = new AreWeUpClientConfig(Bucket, Key)
            {
                SNSTopicArn = Environment.GetEnvironmentVariable("SNSTopic"),
                DefaultCustomerId = Environment.GetEnvironmentVariable("CustomerId")
            };

            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("Subject")))
            {
                ClientConfig.DefaultSubject = Environment.GetEnvironmentVariable("Subject");
            }
            else
            {
                ClientConfig.DefaultSubject = String.Empty;
            }

            //If we can't parse the variable, or it's not present, defaults to false
            if (Boolean.TryParse(Environment.GetEnvironmentVariable("IgnoreSslCertificateErrors"), out bool TempBool))
            {
                ClientConfig.DefaultIgnoreSslErrors = TempBool;
            }

            if (Boolean.TryParse(Environment.GetEnvironmentVariable("SendToCW"), out TempBool))
            {
                ClientConfig.DefaultSendToCloudWatch = TempBool;
            }

            if (Boolean.TryParse(Environment.GetEnvironmentVariable("ForceRefresh"), out TempBool))
            {
                ClientConfig.ForceRefresh = TempBool;
            }

            if (Int32.TryParse(Environment.GetEnvironmentVariable("DefaultTimeout"), out int Timeout))
            {
                if (Timeout > 0)
                {
                    ClientConfig.DefaultTimeout = Timeout;
                }
            }

            //Doing this here means we can load the S3 config once on initialization and only
            //reread it if the environment variable for force refresh is set
            this.SetupClient(ClientConfig).Wait();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// A Lambda function to respond to scheduled events to perform endpoint health checks
        /// </summary>
        /// <param name="event">The scheduled event that triggered the lambda function</param>
        public async Task ExecuteHealthChecks(CloudWatchScheduledEvent @event, ILambdaContext context)
        {
            this._Context = context;
            this._Context.LogInfo($"Running health checks.");

            if (TestEnvironmentVariables())
            {
                this._Client.SetLambdaContext(this._Context);
                await this._Client.Execute();
                this._Context.LogInfo("Finished execution.");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets up the AreWeUpClient and also loads the health check config into the client
        /// </summary>
        /// <param name="config">The config to use with the client</param>
        /// <returns></returns>
        private async Task SetupClient(AreWeUpClientConfig config)
        {
            this._Client = await AreWeUpClient.CreateClient(config);
        }

        /// <summary>
        /// Tests to ensure the private fields were set from the environment variables during construction
        /// and will log any that were not present in CloudWatch
        /// </summary>
        /// <returns>True if all of the environment variables were present, false otherwise</returns>
        private bool TestEnvironmentVariables()
        {
            bool Success = true;

            if (String.IsNullOrEmpty(this._Client.Configuration.S3Bucket))
            {
                this._Context.LogError($"The ConfigBucket environment variable was null or empty.");
                Success = false;
            }

            if (String.IsNullOrEmpty(this._Client.Configuration.S3Key))
            {
                this._Context.LogError($"The ConfigKey environment variable was null or empty.");
                Success = false;
            }

            if (String.IsNullOrEmpty(this._Client.Configuration.SNSTopicArn))
            {
                this._Context.LogError($"You must specify a default SNS topic ARN.");
                Success = false;
            }

            if (String.IsNullOrEmpty(this._Client.Configuration.DefaultCustomerId) && this._Client.Configuration.DefaultSendToCloudWatch)
            {
                this._Context.LogError($"The default CustomerId environment variable was null or empty and sending metrics to CloudWatch by default was specified.");
                Success = false;
            }

            return Success;
        }

        #endregion
    }
}
