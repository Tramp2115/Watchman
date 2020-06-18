﻿using Microsoft.AspNetCore.Mvc;

namespace Watchman.Web.Areas
{
    [ApiController]
    [Produces("application/json")]
    [Route("api/[controller]/[action]")]
    public abstract class BaseApiController : ControllerBase
    {
    }
}
