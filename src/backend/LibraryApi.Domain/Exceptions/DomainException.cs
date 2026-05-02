namespace LibraryApi.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object id)
        : base($"{entity} with id '{id}' was not found.") { }
}

public class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message) { }
}

public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}
