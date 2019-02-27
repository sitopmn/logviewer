using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logviewer.query
{
    /// <summary>
    /// Interface for capturing parsed input
    /// </summary>
    /// <typeparam name="T">Type of the parsed value</typeparam>
    internal interface ICapture<T>
    {
        /// <summary>
        /// Gets the parsed value
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Gets the text parsed into the value
        /// </summary>
        string Text { get; }
    }

    /// <summary>
    /// Extensions to the sprache parser combinator
    /// </summary>
    internal static class SpracheExtensions
    {
        /// <summary>
        /// Capture result implementation class
        /// </summary>
        /// <typeparam name="T">Type of the result value</typeparam>
        private class CaptureResult<T> : ICapture<T>
        {
            /// <summary>
            /// Gets the parsed value
            /// </summary>
            public T Value { get; }

            /// <summary>
            /// Gets the text parsed into the value
            /// </summary>
            public string Text { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="CaptureResult{T}"/> class
            /// </summary>
            /// <param name="value">Parsed value</param>
            /// <param name="text">Text parsed into the value</param>
            public CaptureResult(T value, string text)
            {
                Value = value;
                Text = text;
            }
        }

        /// <summary>
        /// Captures the input while parsing
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="parser">Parser to capture on</param>
        /// <returns><see cref="ICapture{T}"/> containing the parsed value and the input</returns>
        public static Parser<ICapture<T>> Captured<T>(this Parser<T> parser)
        {
            return i =>
            {
                var result = parser(i);
                if (result.WasSuccessful)
                {
                    return Result.Success<ICapture<T>>(new CaptureResult<T>(result.Value, i.Source.Substring(i.Position, result.Remainder.Position - i.Position)), result.Remainder);
                }
                else
                {
                    return Result.Failure<ICapture<T>>(result.Remainder, result.Message, result.Expectations);
                }
            };
        }
    }
}
