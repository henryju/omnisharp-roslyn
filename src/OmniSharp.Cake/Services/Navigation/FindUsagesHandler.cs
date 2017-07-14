using System.Composition;
using System.Threading.Tasks;
using OmniSharp.Mef;
using OmniSharp.Models;
using OmniSharp.Cake.Extensions;
using OmniSharp.Models.FindUsages;

namespace OmniSharp.Cake.Services.Navigation
{
    [OmniSharpHandler(OmniSharpEndpoints.FindUsages, Constants.LanguageNames.Cake), Shared]
    public class FindUsagesHandler : CakeRequestHandler<FindUsagesRequest, QuickFixResponse>
    {
        [ImportingConstructor]
        public FindUsagesHandler(
            OmniSharpWorkspace workspace)
            : base(workspace)
        {
        }

        protected override Task<QuickFixResponse> TranslateResponse(QuickFixResponse response, FindUsagesRequest request)
        {
            return response.TranslateAsync(Workspace, request);
        }
    }
}
