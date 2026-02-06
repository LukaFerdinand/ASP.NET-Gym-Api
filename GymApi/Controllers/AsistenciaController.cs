using GymApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GymApi.Controllers
{
    [Route("api/asistencias")]
    [ApiController]
    public class AsistenciaController : ControllerBase
    {
        private readonly string _connectionString;

        public AsistenciaController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("GymConnectionString");
        }

        [HttpGet("{idMiembro}")]
        public IActionResult GetAsistencia(int idMiembro)
        {
            List<AsistenciaRequest> lista = new List<AsistenciaRequest>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT idAsistencia, fechaHoraEntrada FROM Asistencia WHERE idMiembro = @idMiembro ORDER BY fechaHoraEntrada DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idMiembro", idMiembro);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new AsistenciaRequest
                        {
                            idAsistencia = Convert.ToInt32(reader["idAsistencia"]),
                            idMiembro = idMiembro,
                            fechaHoraEntrada = (Convert.ToDateTime(reader["fechaHoraEntrada"]))
                        });
                    }
                }
            }
            return Ok(lista);
        }

        [HttpPost]
        public IActionResult RegistrarAsistencia([FromBody] AsistenciaRequest request)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Usamos GETDATE() de SQL para que la hora sea la del servidor
                string sql = @"
                    IF NOT EXISTS (
                        SELECT 1 FROM Asistencia 
                        WHERE idMiembro = @idMiembro 
                        AND CAST(fechaHoraEntrada AS DATE) = CAST(GETDATE() AS DATE)
                    )
                    BEGIN
                        INSERT INTO Asistencia (idMiembro, fechaHoraEntrada) 
                        VALUES (@idMiembro, GETDATE());
                    END
                    "; // Para obtener el ID generado

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@idMiembro", request.idMiembro);

                try
                {
                    conn.Open();
                    var idGenerado = cmd.ExecuteScalar();

                    return Ok(new
                    {
                        Message = "Asistencia registrada correctamente",
                        IdAsistencia = idGenerado
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "Error al registrar asistencia: " + ex.Message);
                }
            }
        }

        [HttpGet("verificar/{idMiembro}")]
         public IActionResult VerificarAsistenciaHoy(int idMiembro)
{
         using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        string sql = @"SELECT COUNT(*) FROM Asistencia 
                       WHERE idMiembro = @idMiembro 
                       AND CAST(fechaHoraEntrada AS DATE) = CAST(GETDATE() AS DATE)";
        
        SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@idMiembro", idMiembro);
        
        conn.Open();
        int conteo = (int)cmd.ExecuteScalar();
        
        // Devolvemos true si ya existe al menos un registro hoy
        return Ok(conteo > 0);
    }
}
        
    }
}

