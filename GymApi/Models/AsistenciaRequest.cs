namespace GymApi.Models
{
    public class AsistenciaRequest
    {
        public int idAsistencia {  get; set; }
        public int idMiembro { get; set; }

        public DateTime fechaHoraEntrada { get; set; }
    }
}
