namespace TravelOptimizer.Domain.Exceptions;

/// <summary>Thrown when a requested resource does not exist. Mapped to HTTP 404 (ERRORS.md).</summary>
public class NotFoundException(string message) : Exception(message);
