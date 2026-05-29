using System;
using System.Collections.Generic;
using System.Text;

namespace ProductApi.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

// 404
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string resource, object key)
        : base($"{resource} con id '{key}' no fue encontrado.") { }
}

// 409
public sealed class ConcurrencyException : DomainException
{
    public ConcurrencyException()
        : base("El recurso fue modificado por otro proceso. Vuelve a intentarlo.") { }
}

// 400
public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message) { }
}