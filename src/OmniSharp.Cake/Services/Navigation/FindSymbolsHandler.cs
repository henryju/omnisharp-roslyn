using System.Composition;
using System.Threading.Tasks;
using OmniSharp.Mef;
using OmniSharp.Models;
using OmniSharp.Cake.Extensions;
using OmniSharp.Models.FindSymbols;

namespace OmniSharp.Cake.Services.Navigation
{
    [OmniSharpHandler(OmniSharpEndpoints.FindSymbols, Constants.LanguageNames.Cake), Shared]
    public class FindSymbolsHandler : CakeRequestHandler<FindSymbolsRequest, QuickFixResponse>
    {
        [ImportingConstructor]
        public FindSymbolsHandler(OmniSharpWorkspace workspace)
            : base(workspace)
        {
        }

        protected override Task<QuickFixResponse> TranslateResponse(QuickFixResponse response, FindSymbolsRequest request)
        {
            return response.TranslateAsync(Workspace);
        }
    }
}
