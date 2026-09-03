using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PickleHub.Common.ValueObjects
{
    public sealed class Slug
    {
        public string Value { get; }

        private Slug(string value)
        {
            Value = value;
        }

        public static Slug Create(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input d? t?o slug không du?c d? tr?ng.");

            var slug = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);

            slug = new string(
                slug.Where(c => CharUnicodeInfo.GetUnicodeCategory(c)
                                != UnicodeCategory.NonSpacingMark)
                    .ToArray()
            ).Normalize(NormalizationForm.FormC);

            slug = slug.Replace("d", "d").Replace("Ð", "d");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
            slug = Regex.Replace(slug, @"-{2,}", "-").Trim('-');

            if (string.IsNullOrEmpty(slug))
                throw new ArgumentException("Không th? t?o slug h?p l? t? input dã cho.");

            return new Slug(slug);
        }

        // Dùng khi d?c t? DB ra — dã là slug h?p l?, không c?n x? lý l?i
        public static Slug FromPersistedValue(string value) => new(value);

        public Slug AppendSuffix(int suffix) => new($"{Value}-{suffix}");

        public override string ToString() => Value;
        public override bool Equals(object? obj) => obj is Slug other && Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();
    }
}
