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
    public class MemberRoutinesController : ControllerBase
    {
        private readonly string _connectionString;

        public MemberRoutinesController(IConfiguration configuration)
        {
            // Leemos la cadena de conexión directamente desde appsettings.json
            _connectionString = configuration.GetConnectionString("GymConnectionString");
        }

        // GET: api/memberroutines/{dni}
        [HttpGet("{id}")]
        public List<MemberRoutines> ObtenerRutina(int id)
        {
            var lista = new List<MemberRoutines>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Aquí pegas la consulta JOIN que hicimos antes
                string sql = "SELECT DISTINCT M.idMiembro, M.nombre AS Miembro, TR.nombre AS NombreRutina,  RD.diaSemana,  E.nombre AS Ejercicio,   RD.series,   RD.repeticiones, RD.nota FROM Miembros M INNER JOIN MiembroRutinaAsignada MRA ON M.idMiembro = MRA.idMiembro INNER JOIN TipoRutina TR ON MRA.idTipoRutina = TR.idTipoRutina INNER JOIN RutinaDetalle RD ON TR.idTipoRutina = RD.idTipoRutina INNER JOIN Ejercicios E ON RD.idEjercicio = E.idEjercicio WHERE M.idMiembro = @id;"
                    ;
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new MemberRoutines
                        {
                            Miembro = reader["Miembro"].ToString(),
                            NombreRutina = reader["NombreRutina"].ToString(),
                            DiaSemana = reader["diaSemana"].ToString(),
                            Ejercicio = reader["Ejercicio"].ToString(),
                            Series = Convert.ToInt32(reader["series"]),
                            Repeticiones = reader["repeticiones"].ToString(),
                            Nota = reader["nota"].ToString()
                        });
                    }
                }
            }
            return lista;
        }
    }
}
