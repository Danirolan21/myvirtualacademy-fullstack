namespace MyVirtualAcademy.Models
{
    public class AsignaturaDetalleViewModel
    {
        public int IdAsignatura { get; set; }
        public string NombreAsignatura { get; set; } = string.Empty;
        public List<ProfesorViewModel> Profesores { get; set; } = [];
        public List<TemaViewModel> Temas { get; set; } = [];
    }
}
