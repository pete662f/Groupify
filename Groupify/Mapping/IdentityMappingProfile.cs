using AutoMapper;
using Groupify.Models.Identity;
using Groupify.ViewModels;
namespace Groupify.Mapping;

public class IdentityMappingProfile : Profile
{
    public IdentityMappingProfile()
    {
        CreateMap<RegisterViewModel, ApplicationUser>()
            .ForMember(u => u.UserName, opt => opt.MapFrom(vm => vm.Email))
            .ForMember(u => u.Email, opt => opt.MapFrom(vm => vm.Email))
            .ForMember(u => u.FirstName, opt => opt.MapFrom(vm => vm.FirstName))
            .ForMember(u => u.LastName, opt => opt.MapFrom(vm => vm.LastName));
    }
}