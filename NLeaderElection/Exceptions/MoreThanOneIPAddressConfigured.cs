using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NLeaderElection.Exceptions
{
    public class MoreThanOneIPAddressConfigured : Exception
    {
        public MoreThanOneIPAddressConfigured(string msg) : base (msg)
        {}

        public MoreThanOneIPAddressConfigured() : base ()
        {}
    }
}
