﻿using System;
using Orcus.Server.Connection;

namespace Orcus.Administration.Core.Exceptions
{
    public abstract class RestException : Exception
    {
        protected RestException(RestError error) : base(error.Message)
        {
            ErrorId = error.Code;
        }

        public int ErrorId { get; }
    }
}