using System.Collections.Generic;

namespace api {
    public interface IDeviceParent: IRequest {}
    public interface IChainParent: IRequest {}

    public interface IRequest {
        string Request(string type, Dictionary<string, object> content);
    }
}