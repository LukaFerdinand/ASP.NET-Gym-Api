using GymApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GymApi.Controllers
{
    [Route("api/reservas")]
    [ApiController]
    public class ReservaController : ControllerBase
    {

        private readonly string _connectionString;

        public ReservaController(IConfiguration configuration)
        {
            // Leemos la cadena de conexión directamente desde appsettings.json
            _connectionString = configuration.GetConnectionString("GymConnectionString");
        }


        [HttpPost]
        public IActionResult ReservarClase([FromBody] ReservaRequest request)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // Iniciamos una transacción
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // 1. Verificar si hay cupos disponibles de forma segura
                    string sqlCheck = "SELECT cuposDisponibles FROM SesionClase WHERE idSesion = @idSesion";
                    SqlCommand cmdCheck = new SqlCommand(sqlCheck, conn, transaction);
                    cmdCheck.Parameters.AddWithValue("@idSesion", request.idSesion);

                    // Guardamos el resultado en un object primero
                    object result = cmdCheck.ExecuteScalar();

                    // Si result es null, significa que el idSesion no existe
                    if (result == null)
                    {
                        transaction.Rollback(); // Importante deshacer si usas transacciones
                        return NotFound($"No se encontró la sesión con ID {request.idSesion}");
                    }

                    // Ahora sí es seguro convertirlo
                    int cupos = Convert.ToInt32(result);

                    if (cupos <= 0)
                    {
                        transaction.Rollback();
                        return BadRequest("Lo sentimos, ya no quedan cupos para esta clase.");
                    }

                    // 2. Insertar la reserva
                    string sqlInsert = @"INSERT INTO Reserva (idSesion, idMiembro, fechaReserva) 
                                 VALUES (@idSesion, @idMiembro, GETDATE())";
                    SqlCommand cmdInsert = new SqlCommand(sqlInsert, conn, transaction);
                    cmdInsert.Parameters.AddWithValue("@idSesion", request.idSesion);
                    cmdInsert.Parameters.AddWithValue("@idMiembro", request.idMiembro);
                    cmdInsert.ExecuteNonQuery();

                    // 3. Restar 1 al cupo disponible
                    string sqlUpdate = "UPDATE SesionClase SET cuposDisponibles = cuposDisponibles - 1 WHERE idSesion = @idSesion";
                    SqlCommand cmdUpdate = new SqlCommand(sqlUpdate, conn, transaction);
                    cmdUpdate.Parameters.AddWithValue("@idSesion", request.idSesion);
                    cmdUpdate.ExecuteNonQuery();

                    // Si todo salió bien, confirmamos los cambios
                    transaction.Commit();
                    return Ok(new { message = "Reserva realizada con éxito." });
                }
                catch (Exception ex)
                {
                    // Si algo falló (ej: error de red, base de datos), deshacemos todo
                    transaction.Rollback();
                    return StatusCode(500, "Error interno: " + ex.Message);
                }
            }
        }

        [HttpDelete("{idReserva}")]
        public IActionResult CancelarReserva(int idReserva)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // 1. Obtener el idSesion antes de borrar la reserva (lo necesitamos para devolver el cupo)
                    string sqlGetSesion = "SELECT idSesion FROM Reserva WHERE idReserva = @idReserva";
                    SqlCommand cmdGet = new SqlCommand(sqlGetSesion, conn, transaction);
                    cmdGet.Parameters.AddWithValue("@idReserva", idReserva);

                    var result = cmdGet.ExecuteScalar();
                    if (result == null) return NotFound("La reserva no existe.");
                    int idSesion = (int)result;

                    // 2. Eliminar la reserva
                    string sqlDelete = "DELETE FROM Reserva WHERE idReserva = @idReserva";
                    SqlCommand cmdDel = new SqlCommand(sqlDelete, conn, transaction);
                    cmdDel.Parameters.AddWithValue("@idReserva", idReserva);
                    cmdDel.ExecuteNonQuery();

                    // 3. Devolver el cupo a la tabla SesionClase
                    string sqlUpdate = "UPDATE SesionClase SET cuposDisponibles = cuposDisponibles + 1 WHERE idSesion = @idSesion";
                    SqlCommand cmdUpd = new SqlCommand(sqlUpdate, conn, transaction);
                    cmdUpd.Parameters.AddWithValue("@idSesion", idSesion);
                    cmdUpd.ExecuteNonQuery();

                    transaction.Commit();
                    return Ok("Reserva cancelada y cupo liberado.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, "Error al cancelar: " + ex.Message);
                }
            }
        }
    }
}
