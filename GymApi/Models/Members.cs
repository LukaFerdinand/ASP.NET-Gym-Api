namespace GymApi.Models
{
    public class Members
    {
        // Propiedades públicas para Data Binding y acceso a datos
        public int idMiembro { get; set; }

        public int idGenero { get; set; }
        public string nombre { get; set; }
        public string apellido { get; set; }
        public int diasRestantes { get; set; }
        public DateOnly fechaDeMembresia { get; set; }
        public string miembroEmail { get; set; }

        public string nombreGenero { get; set; }

        public byte[] foto { get; set; }




    }
}
