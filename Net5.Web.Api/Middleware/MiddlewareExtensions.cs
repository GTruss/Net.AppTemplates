using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;

namespace Net5.Web.Api.Middleware {
    public static class MiddlewareExtensions {
        public static IApplicationBuilder UseHeaderMiddleware(this IApplicationBuilder builder) {
            return builder.UseMiddleware<HeaderMiddleware>();
        }
    }
}
