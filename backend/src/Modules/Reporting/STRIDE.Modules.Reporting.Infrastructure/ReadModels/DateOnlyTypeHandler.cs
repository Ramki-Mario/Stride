using System.Data;
using System.Globalization;
using Dapper;

namespace STRIDE.Modules.Reporting.Infrastructure.ReadModels;

/// <summary>
/// Teaches Dapper how to map a SQL <c>DATE</c> column (which ADO.NET surfaces as
/// <see cref="DateTime"/>) to the C# <see cref="DateOnly"/> type used in read models.
///
/// Registered once at startup via <see cref="SqlMapper.AddTypeHandler{T}"/> inside
/// <see cref="ReportingInfrastructureExtensions.AddReportingInfrastructure"/>.
/// </summary>
internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public static readonly DateOnlyTypeHandler Instance = new();

    private DateOnlyTypeHandler() { }

    public override DateOnly Parse(object value) =>
        value switch
        {
            DateTime dt  => DateOnly.FromDateTime(dt),
            DateOnly d   => d,
            string s     => DateOnly.Parse(s, CultureInfo.InvariantCulture),
            _            => throw new InvalidCastException(
                $"Cannot convert {value.GetType().Name} to DateOnly.")
        };

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value  = value.ToDateTime(TimeOnly.MinValue);
    }
}
