using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Westwind.RazorHosting;

namespace Miso.SampleRazorFunctions
{
    public static class Function1
    {
        private static string compiledId = "";
        private static RazorEngine<RazorTemplateBase> host = new RazorEngine<RazorTemplateBase>();

        [FunctionName(nameof(Function1))]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, 
            TraceWriter log,
            ExecutionContext context)
        {
            //if you need:
            //host.AddAssembly(Path.Combine(context.FunctionAppDirectory, $@"bin\System.ValueTuple.dll"));
            //host.AddNamespace("Microsoft.Azure.WebJobs.Host");

            if (string.IsNullOrEmpty(compiledId))
            {
                compiledId = await Helper.GetCompileId(host, context, "Page.cshtml");
            }

            log.Info($"compileId:{compiledId}");

            if (!string.IsNullOrEmpty(host.ErrorMessage))
            {
                log.Error(host.ErrorMessage);
                return req.CreateResponse(HttpStatusCode.InternalServerError, host.ErrorMessage);
            }

            // parse query parameter
            string nameInQueryParameter = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
                .Value;

            // Get request body
            var formData = await req.Content.ReadAsFormDataAsync();
            var nameInBody = formData == null ? "" : formData["name"];

            var model = new Data()
            {
                NameInQuery = nameInQueryParameter,
                NameInBody = nameInBody,
            };

            string result = host.RenderTemplateFromAssembly(compiledId, model);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(result, Encoding.UTF8, "text/html")
            };
        }
    }

    public class Data
    {
        public string NameInQuery { get; set; }
        public string NameInBody { get; set; }
    }

    public class Helper
    {
        public static async Task<string> GetCompileId(RazorEngine<RazorTemplateBase> host, ExecutionContext context, string csHtmlFileName)
        {
            string template;
            string templatePath = Path.Combine(context.FunctionAppDirectory, $@"templates\{csHtmlFileName}");
            using (var reader = File.OpenText(templatePath))
            {
                template = await reader.ReadToEndAsync();
            }
            return host.CompileTemplate(template);
        }
    }
}
