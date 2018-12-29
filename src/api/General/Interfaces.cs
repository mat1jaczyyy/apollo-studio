using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

namespace api {
    public interface IDeviceParent: IRequest {}
    public interface IChainParent: IRequest {}

    public interface IRequest {
        string Request(string type, Dictionary<string, object> content);
    }

    public interface IResponse {
        ObjectResult Respond(string obj, string[] path, Dictionary<string, object> data);
    }
}