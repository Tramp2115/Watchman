﻿using System;
using System.Collections.Generic;
using System.Text;
using Watchman.Cqrs;

namespace Watchman.DomainModel.Messages.Queries
{
    public class GetMessagesQuery : IQuery<GetMessagesQueryResult>
    {
    }
}
