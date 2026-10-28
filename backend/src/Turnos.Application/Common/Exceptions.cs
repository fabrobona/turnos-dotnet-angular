namespace Turnos.Application.Common;

/// <summary>El recurso pedido no existe (HTTP 404).</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>La operación choca con el estado actual, por ejemplo un horario ocupado (HTTP 409).</summary>
public class ConflictException(string message) : Exception(message);
