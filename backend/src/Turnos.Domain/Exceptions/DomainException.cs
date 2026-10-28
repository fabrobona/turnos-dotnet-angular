namespace Turnos.Domain.Exceptions;

/// <summary>Se lanza cuando se viola una regla de negocio del dominio.</summary>
public class DomainException(string message) : Exception(message);
