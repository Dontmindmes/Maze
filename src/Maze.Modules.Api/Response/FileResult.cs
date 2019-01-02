using System;
using Microsoft.Net.Http.Headers;

namespace Maze.Modules.Api.Response
{
    /// <summary>
    ///     Represents an <see cref="FileResult" /> that when executed will
    ///     write a file as the response.
    /// </summary>
    public abstract class FileResult : ActionResult
    {
        private string _fileDownloadName;

        /// <summary>
        ///     Creates a new <see cref="FileResult" /> instance with
        ///     the provided <paramref name="contentType" />.
        /// </summary>
        /// <param name="contentType">The Content-Type header of the response.</param>
        protected FileResult(string contentType)
        {
            ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        }

        /// <summary>Gets the Content-Type header for the response.</summary>
        public string ContentType { get; }

        /// <summary>
        ///     Gets the file name that will be used in the Content-Disposition header of the response.
        /// </summary>
        public string FileDownloadName
        {
            get => _fileDownloadName ?? string.Empty;
            set => _fileDownloadName = value;
        }

        /// <summary>
        ///     Gets or sets the last modified information associated with the <see cref="FileResult" />.
        /// </summary>
        public DateTimeOffset? LastModified { get; set; }

        /// <summary>
        /// Gets or sets the etag associated with the <see cref="FileResult"/>.
        /// </summary>
        public EntityTagHeaderValue EntityTag { get; set; }

        /// <summary>
        ///     Gets or sets the value that enables range processing for the <see cref="FileResult" />.
        /// </summary>
        public bool EnableRangeProcessing { get; set; }
    }
}