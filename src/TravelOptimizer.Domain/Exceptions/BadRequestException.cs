namespace TravelOptimizer.Domain.Exceptions;

/// <summary>Thrown for invalid requests/state. Mapped to HTTP 400 (ERRORS.md).</summary>
public class BadRequestException(string message) : Exception(message);
