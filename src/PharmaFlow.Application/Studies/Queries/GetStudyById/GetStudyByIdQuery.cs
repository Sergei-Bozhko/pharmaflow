using PharmaFlow.Application.Common.Messaging;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Application.Studies.Queries.GetStudyById;

public sealed record GetStudyByIdQuery(StudyId Id) : IAppQuery<StudyDto>;