using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models
{
    /// <summary>
    /// Refresh Token — يسمح للمستخدم بتجديد الـ Access Token بدون Login جديد.
    /// صالح لمدة طويلة (7 أيام) لكنه مربوط بالمستخدم وممكن يُلغى.
    /// </summary>
    [Table("refresh_tokens", Schema = "erp")]
    public class RefreshToken
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>
        /// الـ User اللي التوكن بتاعه.
        /// </summary>
        [Required]
        [Column("user_id")]
        public long UserId { get; set; }

        /// <summary>
        /// التوكن نفسه — قيمة عشوائية طويلة (Base64).
        /// Unique — مفيش توكنين متطابقين.
        /// </summary>
        [Required]
        [Column("token")]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// وقت انتهاء صلاحية الـ Refresh Token.
        /// </summary>
        [Required]
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// وقت الإنشاء.
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// وقت الإلغاء (لو اتعمل Logout أو اتستبدل).
        /// لو NULL → لسه صالح.
        /// </summary>
        [Column("revoked_at")]
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// التوكن الجديد اللي استبدل ده (للتتبع الأمني).
        /// </summary>
        [Column("replaced_by_token")]
        [MaxLength(500)]
        public string? ReplacedByToken { get; set; }

        /// <summary>
        /// السبب: Logout / Expired / Replaced
        /// </summary>
        [Column("revoked_reason")]
        [MaxLength(100)]
        public string? RevokedReason { get; set; }

        /// <summary>
        /// IP اللي اتعمل منه التوكن (اختياري - للتتبع).
        /// </summary>
        [Column("created_by_ip")]
        [MaxLength(50)]
        public string? CreatedByIp { get; set; }

        // ===== Navigation =====

        [ForeignKey(nameof(UserId))]
        public virtual AppUser? User { get; set; }

        // ===== Computed =====

        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        [NotMapped]
        public bool IsRevoked => RevokedAt != null;

        [NotMapped]
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}