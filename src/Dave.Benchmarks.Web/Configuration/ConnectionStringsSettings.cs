using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace Dave.Benchmarks.Web.Configuration;

public class ConnectionStringsSettings
{
    [Required]
    public string DefaultConnection { get; set; } = string.Empty;

    public void Validate()
    {
        Validator.ValidateObject(this, new ValidationContext(this), true);

        var builder = new DbConnectionStringBuilder();
        try
        {
            builder.ConnectionString = DefaultConnection;
        }
        catch (ArgumentException ex)
        {
            throw new ValidationException("ConnectionStrings:DefaultConnection is not a valid connection string.", ex);
        }
    }
}
