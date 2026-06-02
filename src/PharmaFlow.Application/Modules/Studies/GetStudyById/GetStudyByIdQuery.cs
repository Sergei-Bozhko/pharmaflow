using PharmaFlow.Application.Common.Messaging;
using PharmaFlow.Application.Modules.Studies.Contracts;
using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Application.Modules.Studies.GetStudyById;

public sealed record GetStudyByIdQuery(StudyId Id) : IAppQuery<StudyDto>;