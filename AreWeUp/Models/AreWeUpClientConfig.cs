using Newtonsoft.Json;
using System;

namespace BAMCIS.AreWeUp.Models
{
    /// <summary>
    /// The configuration options for the AreWeUpClient
    /// </summary>
    public class AreWeUpClientConfig
    {
        #region Private Fields

        /// <summary>
        /// The default timeout for connections, in milliseconds
        /// </summary>
        private static readonly int _DefaultTimeout = 500;

        #endregion

        #region Public Properties

        /// <summary>
        /// The default SNS topic to send messages to in case one isn't specified per health check endpoint
        /// </summary>
        public string SNSTopicArn { get; set; }

        /// <summary>
        /// The S3 Bucket containing the health check configuration
        /// </summary>
        public string S3Bucket { get; }

        /// <summary>
        /// The S3 Key of the object that is the health check configuration
        /// </summary>
        public string S3Key { get; }

        /// <summary>
        /// The default customer Id to use when reporting metrics to CloudWatch in case one is not
        /// defined per health check endpoint
        /// </summary>
        public string DefaultCustomerId { get; set; }

        /// <summary>
        /// The default preference on handling SSL errors when conducting HTTPS health checks, this defaults to false
        /// </summary>
        public bool DefaultIgnoreSslErrors { get; set; }

        /// <summary>
        /// The default behavior for sending metrics to CloudWatch, this defaults to false. If this is set to true, you must
        /// specify a DefaultCustomerId
        /// </summary>
        public bool DefaultSendToCloudWatch { get; set; }

        /// <summary>
        /// Indicates if the client should force a refresh of the health check configuration from S3 when Execute() is called
        /// </summary>
        public bool ForceRefresh { get; set; }

        /// <summary>
        /// Sets the default timeout to use for TCP tests. This timeout is ignored
        /// for HTTP and HTTPS tests.
        /// </summary>
        public int DefaultTimeout { get; set; }

        /// <summary>
        /// Sets the default subject line to be used with email notifications.
        /// </summary>
        public string DefaultSubject { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new AreWeUpClientConfig to download the health check configurations from S3
        /// </summary>
        /// <param name="s3Bucket">The S3 bucket containing the health check configuration file</param>
        /// <param name="s3Key">The key of the S3 object that is the configuration file</param>
        [JsonConstructor]
        public AreWeUpClientConfig(string s3Bucket, string s3Key)
        {
            if (String.IsNullOrEmpty(s3Bucket))
            {
                throw new ArgumentNullException("s3Bucket", "The S3 Bucket to download the health check settings from cannot be null or empty.");
            }

            if (String.IsNullOrEmpty(s3Key))
            {
                throw new ArgumentNullException("s3Key", "The S3 Key to download the health check settings from cannot be null or empty.");
            }

            this.S3Bucket = s3Bucket;
            this.S3Key = s3Key;
            this.DefaultIgnoreSslErrors = false;
            this.DefaultSendToCloudWatch = false;
            this.ForceRefresh = false;
            this.DefaultCustomerId = String.Empty;
            this.DefaultTimeout = _DefaultTimeout;
            this.DefaultSubject = String.Empty;
        }

        #endregion
    }
}
