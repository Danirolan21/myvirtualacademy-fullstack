using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyVirtualAcademy.Models
{
    [Table("Notificaciones")]
    public class Notificacion
    {
        [Key]
        [Column("ID_Notificacion")]
        public int IdNotificacion { get; set; }

        [Column("ID_Usuario")]
        public int IdUsuario { get; set; }

        [Column("Tipo")]
        public string Tipo { get; set; } = string.Empty;

        [Column("Titulo")]
        public string Titulo { get; set; } = string.Empty;

        [Column("Mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [Column("Leida")]
        public bool Leida { get; set; }

        [Column("Fecha_Creacion")]
        public DateTime FechaCreacion { get; set; }

        [Column("Enviado_Por")]
        public int? EnviadoPor { get; set; }

        [Column("ID_Referencia")]
        public int? IdReferencia { get; set; }

        [ForeignKey("IdUsuario")]
        public Usuario Usuario { get; set; } = null!;
    }
}
