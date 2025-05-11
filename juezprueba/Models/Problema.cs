namespace juezprueba.Models
{
    public class Problema
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public List<CasoDePrueba> CasosDePrueba { get; set; }
    }
}
