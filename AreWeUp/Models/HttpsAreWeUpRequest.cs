using System.Net.Http;

namespace BAMCIS.AreWeUp.Models
{
    /// <summary>
    /// Represents an HTTPS health check request
    /// </summary>
    public class HttpsAreWeUpRequest : WebAreWeUpRequest
    {
        #region Public Properties

        /// <summary>
        /// Specify to ignore SSL validation errors during the check
        /// </summary>
        public bool IgnoreSSLErrors { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Builds a generic HTTPS health check
        /// </summary>
        public HttpsAreWeUpRequest() : base(Protocol.HTTPS) { }

        /// <summary>
        /// Creates an HTTPS health check using the specified URL and method
        /// </summary>
        /// <param name="path">The URL to check</param>
        /// <param name="method">The HTTP method used to check the URL</param>
        public HttpsAreWeUpRequest(string path, HttpMethod method) : this(path, 443, method) { }

        /// <summary>
        /// Creates an HTTPS health check using the specified URL, port, and method
        /// </summary>
        /// <param name="path">The URL to check</param>
        /// <param name="port">The port to use</param>
        /// <param name="method">The HTTP method used to check the URL</param>
        public HttpsAreWeUpRequest(string path, int port, HttpMethod method) : base(path, port, method, Protocol.HTTPS) { }

        #endregion
    }
}
