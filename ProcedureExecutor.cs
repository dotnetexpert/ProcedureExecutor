using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace SampleNamespace
{
    /// <summary>
    /// Provides methods to execute stored procedures and handle SQL data operations.
    /// </summary>
    public class ProcedureExecutor
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private SqlConnection _sqlConnection;

        public ProcedureExecutor(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        #region Private Methods

        /// <summary>
        /// Opens a new SQL connection if not already open.
        /// </summary>
        /// <returns>An open SQL connection.</returns>
        private SqlConnection GetSqlConnection()
        {
            if (_sqlConnection == null || _sqlConnection.State != ConnectionState.Open)
            {
                _sqlConnection = new SqlConnection(_connectionString);
                SqlConnection.ClearAllPools();
                _sqlConnection.Open();
            }
            return _sqlConnection;
        }

        /// <summary>
        /// Closes and disposes the SQL connection.
        /// </summary>
        private void CloseSqlConnection()
        {
            if (_sqlConnection != null)
            {
                _sqlConnection.Close();
                _sqlConnection.Dispose();
                SqlConnection.ClearAllPools();
            }
        }

        /// <summary>
        /// Maps a DataRow to a specified object type.
        /// </summary>
        /// <typeparam name="T">The type of object to map to.</typeparam>
        /// <param name="row">The DataRow to map from.</param>
        /// <param name="columnNames">List of column names to map.</param>
        /// <returns>An object of type T.</returns>
        private static T MapDataRowToObject<T>(DataRow row, List<string> columnNames) where T : new()
        {
            T obj = new T();
            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                string columnName = columnNames.Find(name => name.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(columnName))
                {
                    string value = row[columnName]?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        object convertedValue = Convert.ChangeType(value.Replace("$", "").Replace(",", "").Replace("%", ""), Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
                        property.SetValue(obj, convertedValue);
                    }
                }
            }
            return obj;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes a stored procedure and returns the result as a DataTable.
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure.</param>
        /// <param name="parameterNames">Array of parameter names.</param>
        /// <param name="parameterValues">Array of parameter values.</param>
        /// <returns>A DataTable containing the results of the stored procedure.</returns>
        public DataTable ExecuteStoredProcedure(string storedProcedureName, string[] parameterNames = null, string[] parameterValues = null)
        {
            try
            {
                using (var sqlCommand = new SqlCommand(storedProcedureName, GetSqlConnection()) { CommandType = CommandType.StoredProcedure })
                {
                    if (parameterNames != null && parameterValues != null)
                    {
                        for (int i = 0; i < parameterNames.Length; i++)
                        {
                            sqlCommand.Parameters.AddWithValue(parameterNames[i], parameterValues[i] ?? (object)DBNull.Value);
                        }
                    }

                    using (var sqlDataAdapter = new SqlDataAdapter(sqlCommand))
                    {
                        var dataTable = new DataTable();
                        sqlDataAdapter.Fill(dataTable);
                        return dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception as needed
                throw new DataException("Error executing stored procedure.", ex);
            }
            finally
            {
                CloseSqlConnection();
            }
        }

        /// <summary>
        /// Converts a DataTable to a list of objects of type T.
        /// </summary>
        /// <typeparam name="T">The type of objects in the list.</typeparam>
        /// <param name="dataTable">The DataTable to convert.</param>
        /// <returns>A list of objects of type T.</returns>
        public List<T> ConvertDataTableToList<T>(DataTable dataTable) where T : new()
        {
            try
            {
                var columnNames = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName.ToLower()).ToList();
                return dataTable.AsEnumerable().Select(row => MapDataRowToObject<T>(row, columnNames)).ToList();
            }
            catch (Exception ex)
            {
                // Log exception as needed
                throw new DataException("Error converting DataTable to list.", ex);
            }
        }

        /// <summary>
        /// Converts a DataTable to a single object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="dataTable">The DataTable to convert.</param>
        /// <returns>An object of type T.</returns>
        public T ConvertDataTableToObject<T>(DataTable dataTable) where T : new()
        {
            try
            {
                return ConvertDataTableToList<T>(dataTable).FirstOrDefault();
            }
            catch (Exception ex)
            {
                // Log exception as needed
                throw new DataException("Error converting DataTable to object.", ex);
            }
        }

        /// <summary>
        /// Executes a stored procedure without returning any result.
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure.</param>
        /// <param name="parameterNames">Array of parameter names.</param>
        /// <param name="parameterValues">Array of parameter values.</param>
        public void ExecuteNonQuery(string storedProcedureName, string[] parameterNames = null, string[] parameterValues = null)
        {
            try
            {
                using (var sqlCommand = new SqlCommand(storedProcedureName, GetSqlConnection()) { CommandType = CommandType.StoredProcedure })
                {
                    if (parameterNames != null && parameterValues != null)
                    {
                        for (int i = 0; i < parameterNames.Length; i++)
                        {
                            sqlCommand.Parameters.AddWithValue(parameterNames[i], parameterValues[i] ?? (object)DBNull.Value);
                        }
                    }

                    sqlCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Log exception as needed
                throw new DataException("Error executing stored procedure.", ex);
            }
            finally
            {
                CloseSqlConnection();
            }
        }

        /// <summary>
        /// Converts a list of objects to a DataTable.
        /// </summary>
        /// <typeparam name="T">The type of objects in the list.</typeparam>
        /// <param name="items">The list of objects to convert.</param>
        /// <returns>A DataTable representing the list of objects.</returns>
        public DataTable ConvertListToDataTable<T>(List<T> items)
        {
            var dataTable = new DataTable(typeof(T).Name);

            // Add columns based on properties
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                dataTable.Columns.Add(property.Name);
            }

            // Add rows based on property values
            foreach (var item in items)
            {
                var values = properties.Select(p => p.GetValue(item, null)).ToArray();
                dataTable.Rows.Add(values);
            }

            return dataTable;
        }

        #endregion
    }
}