namespace GatumsGourmet.Domain.Exceptions;

public class DomainException(string message) : Exception(message);
public class NotFoundException(string message) : Exception(message);
public class BusinessRuleException(string message) : Exception(message);
