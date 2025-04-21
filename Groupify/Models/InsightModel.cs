using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Groupify.Models;

public class InsightModel
{
    [Key, ForeignKey(nameof(Student))]
    public int StudentModelId { get; set; }

    public int Red { get; set; }
    public int Green { get; set; }
    public int Blue { get; set; }
    public int Yellow { get; set; }

    public StudentUser Student { get; set; } = null!;
}