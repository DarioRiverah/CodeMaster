namespace juezprueba.Models
{
    public class ProblemaResuelto
    {
        public int Id { get; set; }

        public string UsuarioId { get; set; }
        public ApplicationUser Usuario { get; set; }

        public int ProblemaId { get; set; }
        public Problema Problema { get; set; }

        public DateTime FechaResolucion { get; set; }
    }
}
