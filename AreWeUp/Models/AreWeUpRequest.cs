using System;

namespace BAMCIS.AreWeUp.Models
{
    /// <summary>
    /// An abstract class representing a generic health check request
    /// </summary>
    public abstract class AreWeUpRequest
    {
        #region Public Properties

        /// <summary>
        /// The protocol to use to test the availability of the server, website, or service
        /// </summary>
        public Protocol Protocol { get; }

        /// <summary>
        /// If the protocol is HTTP or HTTPs, provide the complete URL to check, otherwise,
        /// provide the IP address or DNS name of the server or service to check
        /// </summary>
        public string Path { get; protected set; }

        /// <summary>
        /// The customer Id this check is associated with
        /// </summary>
        public string CustomerId { get; set; }

        /// <summary>
        /// Specify to send the test result to a custom CloudWatch metric
        /// </summary>
        public bool SendToCloudWatch { get; set; }

        /// <summary>
        /// The SNS topic to notify if the health check fails
        /// </summary>
        public string SNSTopicArn { get; set; }

        /// <summary>
        /// The subject to use for the email delivery
        /// </summary>
        public string Subject { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for the abstract request that sets the target and type of health check
        /// </summary>
        /// <param name="path">The health check target, a URL, IP Address, or DNS host name</param>
        /// <param name="protocol">The type of health check to perform</param>
        protected AreWeUpRequest(string path, Protocol protocol)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path", "The path to check cannot be null or empty.");
            }

            this.Path = path;
            this.Protocol = Protocol;
            this.Subject = String.Empty;
        }

        /// <summary>
        /// Default constructor that the JSON parsing will use
        /// </summary>
        /// <param name="protocol">The type of health check to perform</param>
        protected AreWeUpRequest(Protocol protocol)
        {
            this.Protocol = protocol;
            this.Subject = String.Empty;
        }

        #endregion
    }
}
