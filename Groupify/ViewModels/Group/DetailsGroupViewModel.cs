using System.Numerics;
using Groupify.Models.Domain;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Groupify.ViewModels.Group;

public class DetailsGroupViewModel
{
    public Models.Domain.Group Group { get; set; } = null!;
    public IEnumerable<Models.Identity.ApplicationUser> Users { get; set; } = Array.Empty<Models.Identity.ApplicationUser>();
    public Vector<float> GroupInsight { get; set; } = new Vector<float>(new float[]{ 0, 0, 0, 0 });
}