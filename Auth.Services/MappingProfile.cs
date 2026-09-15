using AutoMapper;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Auth.Services;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Auth.Data.User, ProfileResponse>()
            .ForMember(
                destination => destination.Role,
                option => option.Ignore());
    }
}