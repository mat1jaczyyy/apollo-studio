using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

namespace Apollo.Core {
    public interface IDeviceParent: IRequest {}
    public interface IChainParent: IRequest {}

    public interface IRequest {
        string Request(Dictionary<string, object> data, List<string> path = null);
    }

    public interface IResponse {
        ObjectResult Respond(string obj, string[] path, Dictionary<string, object> data);
    }
}