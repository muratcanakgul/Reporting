using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using ReportingAPI.Models;
using System.Linq;


namespace ReportingAPI.Services
{
    public class ReportService
    {
        private readonly string _connectionString;
        public ReportService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task<ReportResponse> GetFilteredReportAsync(FilteredReportRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                DynamicParameters parameters = new DynamicParameters();
                string baseSql = "";
                switch (request.ReportType)
                {
                    case "Sales":
                        baseSql = @"SELECT s.SaleId, s.ProductId, p.ProductName, s.Quantity, s.SaleDate, s.TotalAmount
                            FROM Sales s
                            INNER JOIN Products p ON s.ProductId = p.ProductId";
                        break;
                    // Yeni rapor tipleri eklenebilir
                    default:
                        throw new NotImplementedException("Unknown report type: " + request.ReportType);
                }

                // Dinamik WHERE
                string whereSql = "";
                if (request.Filters != null && request.Filters.Count > 0)
                {
                    List<string> wheres = new List<string>();
                    int filterIndex = 0;
                    //string[] allowedFields = { "ProductName", "SaleDate", "Quantity", "TotalAmount", "ProductId", "SaleId" };
                    //string[] allowedOperators = { "=", "<", ">", "<=", ">=", "LIKE" };
                    foreach (var filter in request.Filters)
                    {
                        string paramName = "@param" + filterIndex;

                        object paramValue = filter.Value;

                        // JsonElement gelirse çözümle!
                        if (paramValue is System.Text.Json.JsonElement je)
                        {
                            switch (je.ValueKind)
                            {
                                case System.Text.Json.JsonValueKind.String:
                                    paramValue = je.GetString();
                                    break;
                                case System.Text.Json.JsonValueKind.Number:
                                    // Tarih, int veya double olabilir. Tarihleri string olarak gönderiyorsan:
                                    if (DateTime.TryParse(je.GetRawText().Trim('"'), out DateTime dt))
                                        paramValue = dt;
                                    else if (je.TryGetInt32(out int i))
                                        paramValue = i;
                                    else if (je.TryGetDouble(out double d))
                                        paramValue = d;
                                    else
                                        paramValue = je.GetRawText();
                                    break;
                                case System.Text.Json.JsonValueKind.True:
                                case System.Text.Json.JsonValueKind.False:
                                    paramValue = je.GetBoolean();
                                    break;
                                default:
                                    paramValue = je.ToString();
                                    break;
                            }
                        }

                        switch (filter.Operator.ToUpper())
                        {
                            case "LIKE":
                                wheres.Add($"{filter.Field} LIKE {paramName}");
                                parameters.Add(paramName, "%" + paramValue + "%");
                                break;
                            default:
                                wheres.Add($"{filter.Field} {filter.Operator} {paramName}");
                                parameters.Add(paramName, paramValue);
                                break;
                        }
                        filterIndex++;
                    }
                    whereSql = " WHERE " + string.Join(" AND ", wheres);
                }

                string finalSql = baseSql + whereSql;

                // Count için
                string countSql = $"SELECT COUNT(*) FROM ({finalSql}) AS countQuery";

                int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

                finalSql += " ORDER BY 1 OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                parameters.Add("@Offset", (request.Page - 1) * request.PageSize);
                parameters.Add("@PageSize", request.PageSize);

                var rows = await connection.QueryAsync(finalSql, parameters);
                var data = new List<Dictionary<string, object>>();
                foreach (IDictionary<string, object> row in rows)
                    data.Add(new Dictionary<string, object>(row, StringComparer.OrdinalIgnoreCase));

                return new ReportResponse { Data = data, TotalCount = totalCount };
            }
        }
        public async Task<ReportResponse> GetReportAsync(ReportRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                DynamicParameters parameters;
                string sql = BuildSqlForReport(request, out parameters);

                // Count query for pagination
                string countSql = $"SELECT COUNT(*) FROM ({sql}) AS countQuery";
                int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

                // Pagination
                sql += " ORDER BY 1 OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                parameters.Add("Offset", (request.Page - 1) * request.PageSize);
                parameters.Add("PageSize", request.PageSize);

                var rows = await connection.QueryAsync(sql, parameters);
                var data = new List<Dictionary<string, object>>();

                foreach (IDictionary<string, object> row in rows)
                {
                    data.Add(new Dictionary<string, object>(row, StringComparer.OrdinalIgnoreCase));
                }

                return new ReportResponse { Data = data, TotalCount = totalCount };
            }
        }

        private string BuildSqlForReport(ReportRequest request, out DynamicParameters parameters)
        {
            parameters = new DynamicParameters();

            switch (request.ReportType)
            {
                case "SalesByProduct":
                    parameters.Add("ProductId", request.Parameters.ContainsKey("ProductId") ? request.Parameters["ProductId"] : null);
                    parameters.Add("StartDate", request.Parameters.ContainsKey("StartDate") ? request.Parameters["StartDate"] : null);
                    parameters.Add("EndDate", request.Parameters.ContainsKey("EndDate") ? request.Parameters["EndDate"] : null);
                    return @"
                    SELECT s.SaleId, s.ProductId, p.ProductName, s.Quantity, s.SaleDate, s.TotalAmount
                    FROM Sales s
                    INNER JOIN Products p ON s.ProductId = p.ProductId
                    WHERE (@ProductId IS NULL OR s.ProductId = @ProductId)
                      AND (@StartDate IS NULL OR s.SaleDate >= @StartDate)
                      AND (@EndDate IS NULL OR s.SaleDate <= @EndDate)
                ";
                case "SalesByDate":
                    parameters.Add("StartDate", request.Parameters.ContainsKey("StartDate") ? request.Parameters["StartDate"] : null);
                    parameters.Add("EndDate", request.Parameters.ContainsKey("EndDate") ? request.Parameters["EndDate"] : null);
                    return @"
                    SELECT s.SaleId, s.ProductId, p.ProductName, s.Quantity, s.SaleDate, s.TotalAmount
                    FROM Sales s
                    INNER JOIN Products p ON s.ProductId = p.ProductId
                    WHERE (@StartDate IS NULL OR s.SaleDate >= @StartDate)
                      AND (@EndDate IS NULL OR s.SaleDate <= @EndDate)
                ";
                default:
                    throw new NotImplementedException("Unknown report type: " + request.ReportType);
            }
        }
    }
}
