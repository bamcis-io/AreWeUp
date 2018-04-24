using System.Net.Http;

namespace BAMCIS.AreWeUp.Models
{
    /// <summary>
    /// Represents an HTTP health check
    /// </summary>
    public class HttpAreWeUpRequest : WebAreWeUpRequest
    {
        #region Constructors

        /// <summary>
        /// The default constructor used by JSONConvert
        /// </summary>
        public HttpAreWeUpRequest() : base(Protocol.HTTP) { }

        /// <summary>
        /// Creates an HTTP health check request to the specified path and method
        /// </summary>
        /// <param name="path">The URL to check</param>
        /// <param name="method">The method used to check the URL</param>
        public HttpAreWeUpRequest(string path, HttpMethod method) : this(path, 80, method) { }

        /// <summary>
        /// Creates an HTTP health check request to the specified path and port using the specified method
        /// </summary>
        /// <param name="path">The URL to check</param>
        /// <param name="port">The port to use for the check</param>
        /// <param name="method">The method used to check the URL</param>
        public HttpAreWeUpRequest(string path, int port, HttpMethod method) : base(path, port, method, Protocol.HTTP) { }

        #endregion
    }
}
