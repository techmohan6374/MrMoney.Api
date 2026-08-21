using System;

namespace MrMoney.Api.Infrastructure
{
    public static class StringExtensions
    {
        public static string IfEmpty(this string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        public static string? IfEmptyNull(this string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
