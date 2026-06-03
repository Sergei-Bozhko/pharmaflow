using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Application.Modules.Sites.Contracts;

public interface ISitesModule
{
    public Task<int> CountSitesForStudyAsync(StudyId studyId, CancellationToken ct);
}