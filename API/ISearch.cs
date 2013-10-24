using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;

namespace API
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ISearch" in both code and config file together.
    [ServiceContract]
    public interface ISearch
    {
        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "search/{sql}")]
        AjaxDictionary<string,string>[] search(string sql);


        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "search/{sql}/{page}/{taken}")]
        AjaxDictionary<string, string>[] search2(string sql, string page, string taken);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "complete/{query}")]
        List<string> complete(string query);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "get/{code}")]
        AjaxDictionary<string, string>[] get(string code);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped,
            UriTemplate = "getToken/{token}")]
        AjaxDictionary<string, string>[] getToken(string token);
    }
}
