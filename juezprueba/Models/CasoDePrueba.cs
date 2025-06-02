namespace juezprueba.Models
{
    public class CasoDePrueba
    {
        public int Id { get; set; }
        public string? Input { get; set; }
        public string OutputEsperado { get; set; }
        public int ProblemaId { get; set; }
        public Problema Problema { get; set; }
    }
}
