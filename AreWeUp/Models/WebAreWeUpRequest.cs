using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace BAMCIS.AreWeUp.Models
{
    /// <summary>
    /// An abstract class representing a generic HTTP or HTTP health check
    /// </summary>
    public abstract class WebAreWeUpRequest : NetworkAreWeUpRequest
    {
        #region Public Properties

        /// <summary>
        /// For HTTP or HTTPS requests, the HTTP method to use against the URL
        /// </summary>
        public HttpMethod Method { get; private set; }

        /// <summary>
        /// Data that will be sent in PUT, PATCH, or POST requests
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// The content type of the data supplied to the Content parameter, like application/json or text/html or application/x-www-form-urlencoded
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// This prevents the HttpClient from following redirects. If this is specified, you must specify a Key Value pair to be inspected in
        /// the response headers to check for success
        /// </summary>
        public bool PreventAutoRedirect { get; set; }

        /// <summary>
        /// The headers to inspect from a redirect response to gauge success.
        /// </summary>
        public Dictionary<string, string> RedirectHeadersToValidate { get; set; }

        /// <summary>
        /// A list of cookie Ids that need to be present in an HTTP response for it to be considered successful.
        /// </summary>
        public List<string> CookiesToValidate { get; set; }

        /// <summary>
        /// The expected response from the web endpoint, this defaults to 200
        /// </summary>
        public HttpStatusCode ExpectedResponse { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a generic HTTP or HTTP health check
        /// </summary>
        /// <param name="protocol">Either HTTPS or HTTP</param>
        protected WebAreWeUpRequest(Protocol protocol) : this((protocol == Protocol.HTTPS ? 443 : 80), protocol)
        {
        }

        /// <summary>
        /// Creates an HTTP or HTTPS health check using the specified port
        /// </summary>
        /// <param name="port">The port to check on</param>
        /// <param name="protocol">Either HTTP or HTTPS</param>
        protected WebAreWeUpRequest(int port, Protocol protocol) : base(port, protocol)
        {
            this.Method = HttpMethod.Head;
            this.RedirectHeadersToValidate = new Dictionary<string, string>();
            this.CookiesToValidate = new List<string>();
            this.Content = String.Empty;
            this.ContentType = String.Empty;
            this.ExpectedResponse = HttpStatusCode.OK;
        }

        /// <summary>
        /// Create an HTTP or HTTPS health check using the specified URL and method
        /// </summary>
        /// <param name="path">The URL to check</param>
        /// <param name="method">The HTTP method used to check the URL</param>
        /// <param name="protocol">Either HTTP or HTTPS</param>
        protected WebAreWeUpRequest(string path, HttpMethod method, Protocol protocol) : this(path, (protocol == Protocol.HTTPS ? 443 : 80), method, protocol)
        {
        }

        /// <summary>
        /// Creates an HTTP or HTTPS health check using the specified url and port and defaults the method to HEAD
        /// </summary>
        /// <param name="path">The URL to check</param>
        /// <param name="port">The port to use</param>
        /// <param name="protocol">Either HTTP or HTTPS</param>
        protected WebAreWeUpRequest(string path, int port, Protocol protocol) : this(path, port, HttpMethod.Head, protocol)
        {
        }

        /// <summary>
        /// Creates an HTTP or HTTPS health check using the specified url, port, and method.
        /// </summary>
        /// <param name="path">The URL to check</param>
        /// <param name="port">The port to use</param>
        /// <param name="method">The HTTP method used to check the URL</param>
        /// <param name="protocol">Either HTTP or HTTPS</param>
        protected WebAreWeUpRequest(string path, int port, HttpMethod method, Protocol protocol) : base(path, port, protocol)
        {
            this.Method = method;
            this.RedirectHeadersToValidate = new Dictionary<string, string>();
            this.CookiesToValidate = new List<string>();
            this.Content = String.Empty;
            this.ContentType = String.Empty;
            this.ExpectedResponse = HttpStatusCode.OK;

            switch (protocol)
            {
                case Protocol.HTTP:
                    {
                        if (!this.Path.StartsWith("http://"))
                        {
                            this.Path = $"http://{Path}";
                        }

                        break;
                    }
                case Protocol.HTTPS:
                    {
                        if (!this.Path.StartsWith("https://"))
                        {
                            this.Path = $"https://{Path}";
                        }
                        break;
                    }
                default:
                    {
                        throw new ArgumentException($"The specified protocol {protocol.ToString()} is not valid for a web health check.");
                    }
            }

            if (!Uri.IsWellFormedUriString(this.Path, UriKind.RelativeOrAbsolute))
            {
                throw new UriFormatException("The provided path could not be parsed into a valid URI");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves the well formatted URI based on the provided path, port, and protocol.
        /// </summary>
        /// <returns></returns>
        public Uri GetPath()
        {
            string Path = this.Path;

            string Scheme = this.Protocol.ToString().ToLower();

            // Add in the URI scheme
            if (!Path.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
            {
                Path = $"{Scheme}://{Path}";
            }

            // Insert the port if it's not already there or it's not the default port for the protocol
            if (this.Port > 0 && this.Port <= 65535 &&
                Path.IndexOf($":{this.Port}") == -1 &&
                ((Path.StartsWith($"{Scheme}://", StringComparison.OrdinalIgnoreCase) && this.Port != 80) ||
                (Path.StartsWith($"{Scheme}://", StringComparison.OrdinalIgnoreCase) && this.Port != 443))
                )
            {
                Uri Temp = new Uri(Path);

                Path = $"{Temp.Scheme}://{Temp.Host}:{this.Port}{Temp.PathAndQuery}";
            }

            return new Uri(Path);
        }

        /// <summary>
        /// Retrieves the content to be included in the HTTP request for POST, PUT, and PATCH requests
        /// </summary>
        /// <returns></returns>
        public StringContent GetContent()
        {
            if (String.IsNullOrEmpty(this.ContentType))
            {
                return new StringContent(this.Content, Encoding.UTF8);
            }
            else
            {
                return new StringContent(this.Content, Encoding.UTF8, this.ContentType);
            }

        }

        #endregion
    }
}
