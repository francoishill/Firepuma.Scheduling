﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable RedundantAnonymousTypePropertyName

namespace Firepuma.Scheduling.FunctionApp.Infrastructure.HttpResponses;

public static class HttpResponseFactory
{
    public static IActionResult CreateBadRequestResponse(params string[] errors)
    {
        return new BadRequestObjectResult(new Dictionary<string, object>
        {
            { "Errors", errors }
        });
    }
}