using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Studies;

namespace PharmaFlow.Application.Modules.Studies.Contracts;

public interface IStudiesModule
{
    Task<bool> StudyExistsAsync(StudyId studyId, CancellationToken ct);
    Task<StudyDto?> GetStudyByIdAsync(StudyId studyId, CancellationToken ct);
}