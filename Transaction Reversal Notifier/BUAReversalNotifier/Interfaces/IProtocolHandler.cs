using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MbokoReversalNotifier.Helpers.Models;

namespace MbokoReversalNotifier.Interfaces
{
    public interface IProtocolHandler
    {
        public Task<string> HttpPostMethod(string destination, string token, string parameter, string url, bool usenameVParm, objectMultiSelect nameVParam);
    }
}
