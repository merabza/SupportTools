using SupportTools.Domain.Repositories;

namespace SupportTools.Domain.GitIgnoreFileTypes;

public interface IGitIgnoreFileTypeRepository : ICrudRepository<GitIgnoreFileType, GitIgnoreFileTypeId>;