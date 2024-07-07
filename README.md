# OracleEFTools

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Overview

OracleEFTools is a .NET library designed to simplify access to Oracle stored procedures from Entity Framework Core (EF Core). This tool provides a streamlined way to interact with Oracle databases, making it easier to work with complex stored procedures and integrate them into your EF Core workflow.

## Features

- **Easy Integration**: Simplify the process of calling Oracle stored procedures from your EF Core application.
- **Parameter Handling**: Automatically handles input and output parameters.
- **Result Mapping**: Maps stored procedure results to your EF Core entities.
- **Async Support**: Fully supports asynchronous operations.

## Installation

To install OracleEFTools, you can use the NuGet package manager:

```sh
dotnet add package OracleEFTools
```

Or via the Package Manager Console in Visual Studio:

```sh
Install-Package OracleEFTools
```

## Usage

### Custom Oracle Object Type

The following is an example of how to define a custom Oracle object type for mapping the result of the stored procedure:

```csharp
using Oracle.ManagedDataAccess.Types;
using OracleEFTools;

namespace YourNamespace.Domain.Procedures
{
    [OracleCustomTypeMapping("RESERVATION_SCHEMA.RESERVATION_T")]
    public sealed class ReservationType : BaseUdt<ReservationType>
    {
        [OracleObjectMapping("RESERVATION_ID")]
        public int ReservationId { get; set; }

        [OracleObjectMapping("RESERVATION_NAME")]
        public string ReservationName { get; set; }

        [OracleObjectMapping("RESERVATION_DATE")]
        public DateTime ReservationDate { get; set; }

        [OracleObjectMapping("RESERVATION_STATUS")]
        public string ReservationStatus { get; set; }
    }

    public class ReservationParams
    {
        public string GuestName { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string RoomType { get; set; }
        public int NumberOfGuests { get; set; }
        public string SpecialRequests { get; set; }
        public ReservationType ReservationData { get; set; }
    }
}
```

### Configuration

First, configure your `DbContext` to use OracleEFTools. Below is an example of how to set up your `DbContext` and define stored procedures:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using OracleEFTools;
using System.Data;
using System.Data.Common;
using YourNamespace.Domain.Procedures;

namespace YourNamespace.Context
{
    internal class AppDbContext : OracleDbContext
    {
        public static readonly ILoggerFactory DBLoggerFactory = LoggerFactory.Create(builder =>
        {
            builder
            .AddFilter((category, level) =>
                category == DbLoggerCategory.Database.Command.Name
                && level == LogLevel.Information);
        });

        public DBProc<ReservationParams> ExecuteReservationDbProc { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseOracle("<connection string>")
                          .EnableSensitiveDataLogging(true)
                          .UseLoggerFactory(DBLoggerFactory);
        }

        protected override void OnProcedureCall(ProcedureBuilder procedureBuilder)
        {
            procedureBuilder.Procedure<ReservationParams>(proc =>
            {
                proc.ToProcedure("PKG_RESERVATION.EXECUTE_RESERVATION", "RESERVATION_SCHEMA");
                proc.Parameter(x => x.GuestName).In(OracleDbType.Varchar2);
                proc.Parameter(x => x.CheckInDate).In(OracleDbType.Date);
                proc.Parameter(x => x.CheckOutDate).In(OracleDbType.Date);
                proc.Parameter(x => x.RoomType).In(OracleDbType.Varchar2);
                proc.Parameter(x => x.NumberOfGuests).In(OracleDbType.Int32);
                proc.Parameter(x => x.SpecialRequests).In(OracleDbType.Varchar2);
                proc.Parameter(x => x.ReservationData).Out<ReservationType>();
            });
        }
    }
}
```

### Example Functional Test

Here is an example of a functional test using the OracleEFTools library. This demonstrates how to use the defined stored procedures without manually configuring `OracleParameter` objects or directly invoking the stored procedure.

```csharp
[Fact]
public void ExecuteReservationDbProc_Test()
{
    // Arrange
    ReservationParams reservationParams = new()
    {
        GuestName = "John Doe",
        CheckInDate = new DateTime(2024, 7, 20),
        CheckOutDate = new DateTime(2024, 7, 25),
        RoomType = "Deluxe",
        NumberOfGuests = 2,
        SpecialRequests = "Late check-out",
        ReservationData = null
    };

    // Act
    sut.ExecuteReservationDbProc.Execute(reservationParams);
    
    // Assert
    Assert.NotNull(reservationParams.ReservationData);
    reservationParams.ReservationData.ReservationId.Should().Be(123);
    reservationParams.ReservationData.ReservationName.Should().Be("SampleReservation");
}
```

## Contributing

Contributions are welcome! Please submit a pull request or open an issue to discuss your ideas or report bugs.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

## Contact

For any questions or suggestions, feel free to contact the maintainer:

- [Your Name](mailto:your.email@example.com)
- [GitHub](https://github.com/sclark2006)

## Acknowledgements

Special thanks to the contributors and the open-source community for their valuable input and support.

---

Feel free to modify this template according to your project's specific needs.