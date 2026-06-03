using PharmaFlow.Application.Modules.Sites.Contracts;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Application.Modules.Sites.Internal;

internal sealed class SitesModule : ISitesModule
{
    public Task<int> CountSitesForStudyAsync(StudyId studyId)
    {
        throw new NotImplementedException();
    }
}