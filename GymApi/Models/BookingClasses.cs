namespace GymApi.Models
{
    public class BookingClasses
    {

        public int idSesion { get; set; }
        public string nombreClase { get; set; }
        public string instructor {  get; set; }
        public string turno { get; set; }

        public TimeSpan horaInicio { get; set; }

        public DateOnly fecha { get; set; }

        public int cuposDisponibles { get; set; }

        // ESTO ES LO QUE FALTA EN TU API:
        public bool YaReservado { get; set; }
        public int? IdReserva { get; set; }

    }
}
