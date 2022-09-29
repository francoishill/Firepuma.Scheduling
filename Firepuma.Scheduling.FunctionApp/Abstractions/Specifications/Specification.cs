using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Firepuma.Scheduling.FunctionApp.Abstractions.Specifications;

public class Specification<T> : ISpecification<T>
{
    public List<Expression<Func<T, bool>>> WhereExpressions { get; }
        = new List<Expression<Func<T, bool>>>();

    public List<(Expression<Func<T, object>> KeySelector, OrderTypeEnum OrderType)> OrderExpressions { get; }
        = new List<(Expression<Func<T, object>> KeySelector, OrderTypeEnum OrderType)>();

    public int? Take { get; set; } = null;

    public int? Skip { get; set; } = null;
}