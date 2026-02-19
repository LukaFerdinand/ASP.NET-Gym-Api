using Dapper; // Librería recomendada para velocidad en 2 semanas
using GymApi.ContextDB;
using GymApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GimnasioAPI.Controllers
{
    [Route("api/members")]
    [ApiController]
    public class MembersController : ControllerBase
    {
        private readonly string _connectionString;

        public MembersController(IConfiguration configuration)
        {
            // Leemos la cadena de conexión directamente desde appsettings.json
            _connectionString = configuration.GetConnectionString("GymConnectionString");
        }

        // GET: api/Members
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Members> lista = new List<Members>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Consulta simple
                string query = "SELECT idMiembro, idGenero, nombre, apellido, diasRestantes, fechaDeMembresia, miembroEmail, fotoPerfil FROM Miembros";
                SqlCommand cmd = new SqlCommand(query, conn);

                try
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Members
                            {
                                idMiembro = Convert.ToInt32(reader["idMiembro"]),
                                idGenero = Convert.ToInt32(reader["idGenero"]),
                                nombre = reader["nombre"].ToString(),
                                apellido = reader["apellido"].ToString(),
                                diasRestantes = Convert.ToInt32(reader["diasRestantes"]),
                                // SQL maneja 'date', C# recibe DateTime, convertimos a DateOnly
                                fechaDeMembresia = DateOnly.FromDateTime(Convert.ToDateTime(reader["fechaDeMembresia"])),
                                miembroEmail = reader["miembroEmail"].ToString(),
                                foto = reader["fotoPerfil"] as byte[]

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

        // GET: api/members/{dni}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPerfil(int id)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                // Consulta SQL simple
                const string sql = @"
            SELECT 
                M.nombre, M.apellido, M.dni, M.fechaNacimiento, 
                G.nombreGenero, M.miembroEmail,
                M.passwordHash, M.fotoPerfil,
                M.fechaDeMembresia,
                ULTIMA_VENTA.fechaInicio AS FechaInicioMembresia,
                ULTIMA_VENTA.fechaFin AS FechaVencimientoMembresia
            FROM Miembros M
            INNER JOIN Genero G ON M.idGenero = G.idGenero
            OUTER APPLY (
                SELECT TOP 1 V.fechaInicio, V.fechaFin
                FROM VentaMembresia V
                WHERE V.idMiembro = M.idMiembro
                ORDER BY V.fechaFin DESC -- Traemos la venta con el vencimiento más lejano
            ) AS ULTIMA_VENTA
            WHERE M.idMiembro = @id";


                var miembro = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { id });

                if (miembro == null) return NotFound(new { message = "Miembro no encontrado" });

                return Ok(miembro);
            }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest login)
        {
            Console.WriteLine($"Intento de login con: [{login.miembroEmail}] y [{login.password}]");

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // La consulta busca el miembro exacto
                string sql = "SELECT idMiembro, nombre FROM Miembros WHERE TRIM(miembroEmail) = @email AND TRIM(passwordHash) = @pass";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@email", login.miembroEmail);
                cmd.Parameters.AddWithValue("@pass", login.password);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Si lo encuentra, devolvemos un objeto con su ID y Nombre
                        return Ok(new
                        {
                            Id = Convert.ToInt32(reader["idMiembro"]),
                            Nombre = reader["nombre"].ToString()
                        });
                    }
                }
            }
            // Si llegamos aquí, es porque no hubo coincidencia
            
            return Unauthorized("Email o contraseña incorrectos");
        }
    }
}
