using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace nugex
{
    class ErrorMessage : Exception
    {
        public ErrorMessage(string text) : base(text)
        {

        }
    }

    class ExceptionProcessor
    {
        public static List<Type> SupportedExceptions => new()
        {
            typeof(ErrorMessage),        // these are messages and instructions to the user
            typeof(HttpRequestException) // these are well describing NuGet error messages in connection to the asked operation
        };
        public static Exception GetSupportedExcepton(Exception exc)
        {
            var error = exc;
            while (error != null && !SupportedExceptions.Any(se => se == error.GetType())) error = error.InnerException;
            return error;
        }
    }
}
