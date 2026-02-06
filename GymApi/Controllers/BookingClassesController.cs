using Dapper; // Librería recomendada para velocidad en 2 semanas
using GymApi.ContextDB;
using GymApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace GimnasioAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class BookingClassesController : ControllerBase
    {

        private readonly string _connectionString;

        public BookingClassesController(IConfiguration configuration)
        {
            // Leemos la cadena de conexión directamente desde appsettings.json
            _connectionString = configuration.GetConnectionString("GymConnectionString");
        }


        // GET: api/bookingclasses
        [HttpGet("{idMiembro}")]
        public IActionResult GetAllClasses(int idMiembro)
        {
            List<BookingClasses> lista = new List<BookingClasses>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Consulta simple
                /*string query = "SELECT S.idSesion AS idSesion, C.Nombre AS Clase, I.nombre AS Instructor, T.nombre AS Turno, T.horaInicio,  S.fecha, S.cuposDisponibles FROM SesionClase S INNER JOIN Clases C ON S.idClase = C.idClase INNER JOIN InstructorHorario IH ON S.idHorario = IH.idHorario INNER JOIN Instructor I ON IH.idInstructor = I.idInstructor INNER JOIN Turno T ON IH.idTurno = T.idTurno WHERE S.fecha >= CAST(GETDATE() AS DATE)  AND S.cuposDisponibles > 0";
                 * 
                 * */

                string query = "SELECT \r\n    S.idSesion AS idSesion, \r\n    C.Nombre AS Clase, \r\n    I.nombre AS Instructor, \r\n    T.nombre AS Turno, \r\n    T.horaInicio,  \r\n    S.fecha, \r\n    S.cuposDisponibles,\r\n    R.idReserva, -- Si es NULL, no tiene reserva. Si tiene número, ya reservó.\r\n    CASE \r\n        WHEN R.idReserva IS NOT NULL THEN 1 \r\n        ELSE 0 \r\n    END AS YaReservado\r\nFROM SesionClase S\r\nINNER JOIN Clases C ON S.idClase = C.idClase\r\nINNER JOIN InstructorHorario IH ON S.idHorario = IH.idHorario\r\nINNER JOIN Instructor I ON IH.idInstructor = I.idInstructor\r\nINNER JOIN Turno T ON IH.idTurno = T.idTurno\r\n-- AQUÍ ESTÁ EL TRUCO PARA EVITAR DUPLICADOS:\r\nLEFT JOIN Reserva R ON S.idSesion = R.idSesion AND R.idMiembro = @idMiembro\r\nWHERE S.fecha >= CAST(GETDATE() AS DATE)  \r\n  AND (S.cuposDisponibles > 0 OR R.idReserva IS NOT NULL)";

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@idMiembro", idMiembro);

                try
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        int entryTimeOrdinal = reader.GetOrdinal("horaInicio");
                        while (reader.Read())
                        {
                            lista.Add(new BookingClasses
                            {
                                idSesion = Convert.ToInt32(reader["idSesion"]),
                                
                                nombreClase = reader["Clase"].ToString(),
                                instructor = reader["Instructor"].ToString(),
                                turno = reader["Turno"].ToString(),
                                horaInicio = reader.GetTimeSpan(entryTimeOrdinal),
                                
                                fecha = DateOnly.FromDateTime(Convert.ToDateTime(reader["fecha"])),
                                cuposDisponibles = Convert.ToInt32(reader["cuposDisponibles"]),

                                // MAPEO DE LOS NUEVOS CAMPOS:
                                // Convertimos el 1/0 del CASE de SQL a bool
                                YaReservado = Convert.ToInt32(reader["YaReservado"]) == 1,

                                // Si hay reserva traemos el ID, si no, se queda en null
                                IdReserva = reader["idReserva"] != DBNull.Value ? (int?)reader["idReserva"] : null



                            });
                        }
                    }
                    return Ok(lista);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error interno: {ex.Message}");
                }
            }
        }
      
    }
}
