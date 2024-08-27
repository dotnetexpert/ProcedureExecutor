# ProcedureExecutor

## Overview
`ProcedureExecutor` is a class designed to execute stored procedures in a database, using a structured and efficient approach. This class handles the process of connecting to the database, executing the stored procedures, and returning the results.

## Key Features
- **Database Connectivity**: Establishes a connection to the database using a provided connection string.
- **Stored Procedure Execution**: Executes stored procedures with input parameters and fetches results.
- **Error Handling**: Includes mechanisms for handling exceptions and logging errors during procedure execution.

## How It Works
1. **Initialization**: The `ProcedureExecutor` is initialized with a database connection string.
2. **Execution**: The `ExecuteProcedure` method is called with the name of the stored procedure and a set of parameters.
3. **Result Handling**: The result of the stored procedure is returned, either as a scalar value, a list of records, or other data types depending on the procedure's output.

## Example Usage

```csharp
// Initialize the ProcedureExecutor with a connection string
var executor = new ProcedureExecutor("YourConnectionStringHere");

// Define parameters for the stored procedure
var parameters = new List<SqlParameter>
{
    new SqlParameter("@ParameterName", SqlDbType.Int) { Value = 123 }
};

// Execute the stored procedure asynchronously
var result = executor.ExecuteProcedure("StoredProcedureName", parameters);

// Process the result
Console.WriteLine(result);
