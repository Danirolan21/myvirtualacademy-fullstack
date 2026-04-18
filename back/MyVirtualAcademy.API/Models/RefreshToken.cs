using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVirtualAcademy.Models
{
    [Table("RefreshTokens")]
    public class RefreshToken
    {
        [Key]
        [Column("Token")]
        public string Token { get; set; }
        [Column("IdUsuario")]
        public int IdUsuario { get; set; }
        [Column("Expiry")]
        public DateTime Expiry { get; set; }
        [Column("Revoked")]
        public bool Revoked { get; set; }
    }
}
