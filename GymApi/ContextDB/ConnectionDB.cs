using Microsoft.Data.SqlClient;
namespace GymApi.ContextDB

{
    public class ConnectionDB
    {
        private readonly string _connectionString;

        public ConnectionDB(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SqlConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString); 
        }

        

        
    }
}
