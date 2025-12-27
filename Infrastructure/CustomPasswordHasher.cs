using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNet.Identity;

namespace WebApp.Infrastructure
{
    // Versioned PBKDF2-HMAC-SHA256 password hasher with legacy fallback.
    // Format: PBKDF2$SHA256$<iterations>$<saltBase64>$<subkeyBase64>
    public class CustomPasswordHasher : IPasswordHasher
    {
        private const int DefaultIterations = 200000; // 2025 권장 기본값 — 환경에 맞게 조정
        private const int SaltSize = 16;
        private const int KeySize = 32;

        private readonly int _iterations;
        private readonly PasswordHasher _legacyHasher = new PasswordHasher();

        public CustomPasswordHasher() : this(DefaultIterations)
        {
        }

        public CustomPasswordHasher(int iterations)
        {
            _iterations = iterations > 0 ? iterations : DefaultIterations;
        }

        public string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var subkey = PBKDF2_HMACSHA256(Encoding.UTF8.GetBytes(password), salt, _iterations, KeySize);

            // 안전한 버전 태그를 붙여 저장
            return string.Join("$", "PBKDF2", "SHA256", _iterations.ToString(), Convert.ToBase64String(salt), Convert.ToBase64String(subkey));
        }

        public PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword)
        {
            if (hashedPassword == null) return PasswordVerificationResult.Failed;
            if (providedPassword == null) return PasswordVerificationResult.Failed;

            // 신형 포맷 검사
            if (hashedPassword.StartsWith("PBKDF2$SHA256$", StringComparison.Ordinal))
            {
                var parts = hashedPassword.Split('$');
                if (parts.Length != 5) return PasswordVerificationResult.Failed;

                if (!int.TryParse(parts[2], out var iterations)) return PasswordVerificationResult.Failed;
                var salt = Convert.FromBase64String(parts[3]);
                var expectedSubkey = Convert.FromBase64String(parts[4]);

                var actualSubkey = PBKDF2_HMACSHA256(Encoding.UTF8.GetBytes(providedPassword), salt, iterations, expectedSubkey.Length);

                if (FixedTimeEquals(actualSubkey, expectedSubkey))
                {
                    return PasswordVerificationResult.Success;
                }

                return PasswordVerificationResult.Failed;
            }

            // legacy fallback (ASP.NET Identity v2 기본 해시 — PBKDF2-HMAC-SHA1)
            var legacyResult = _legacyHasher.VerifyHashedPassword(hashedPassword, providedPassword);
            if (legacyResult == PasswordVerificationResult.Success)
            {
                // 성공은 했지만 재해시가 필요함을 알림 (Identity가 처리하도록 신호)
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            return PasswordVerificationResult.Failed;
        }

        // Use framework-provided PBKDF2 (Rfc2898DeriveBytes) with SHA256 when available.
        private static byte[] PBKDF2_HMACSHA256(byte[] password, byte[] salt, int iterations, int dkLen)
        {
            // Rfc2898DeriveBytes has an overload that accepts a byte[] password and HashAlgorithmName
            // when targeting .NET Framework 4.7.2+. This delegates the derivation to a tested framework implementation.
            using (var rfc2898 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return rfc2898.GetBytes(dkLen);
            }
        }

        private static byte[] GetIntBlock(int i)
        {
            // Big-endian
            return new[] { (byte)(i >> 24), (byte)(i >> 16), (byte)(i >> 8), (byte)i };
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
